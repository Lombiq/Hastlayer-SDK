using Hast.Layer;
using Hast.Synthesis.Abstractions;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Attribute = ICSharpCode.Decompiler.CSharp.Syntax.Attribute;

namespace Hast.Transformer.Services;

/// <summary>
/// Replaces <see langword="static"/><see langword="readonly"/> fields with literal value substitution. Only applies to
/// fields that have the <see cref="ReplaceableAttribute"/>.
/// </summary>
/// <example>
/// <para>If you have this code:</para>
/// <code>
///     public class ParallelAlgorithm
///     {
///         [Replaceable(nameof(ParallelAlgorithm) + "." + nameof(MaxDegreeOfParallelism))]
///         private static readonly int MaxDegreeOfParallelism = 260;
///         // ..
///         public virtual void Run(SimpleMemory memory)
///         {
///             var input = memory.ReadInt32(Run_InputInt32Index);
///             var tasks = new Task&lt;int&gt;[MaxDegreeOfParallelism];
/// </code>
/// <para>The last line becomes by default:</para>
/// <code>
///             var tasks = new Task&lt;int&gt;[260];
/// </code>
/// <para>But with substitution of 123:</para>
/// <code>
///             var tasks = new Task&lt;int&gt;[123];
/// </code>
/// </example>
public class ReadonlyToConstConverter : IConverter
{
    public IEnumerable<string> Dependencies { get; } = new[] { nameof(SyntaxTreeCleaner) };

    /// <summary>
    /// Finds and replaces any <see langword="public"/><see langword="static"/><see langword="readonly"/> fields in
    /// <paramref name="syntaxTree"/> that have <see cref="ReplaceableAttribute"/> with their assigned literal value. If
    /// <see cref="IHardwareGenerationConfiguration.CustomConfiguration"/> in <paramref name="configuration"/> has a
    /// <c>ReplaceableDynamicConstants</c> of type <c>IDictionary&lt;string, string&gt;</c> with the matching value
    /// identified by the attribute, then it is used instead of the assigned value. Read "Using dynamic constants" in
    /// <c>Docs/DevelopingHastlayer.md</c> for usage information.
    /// </summary>
    /// <param name="syntaxTree">The code to transform.</param>
    /// <param name="configuration">The holder of the custom configuration.</param>
    public void Convert(
        SyntaxTree syntaxTree,
        IHardwareGenerationConfiguration configuration,
        IKnownTypeLookupTable knownTypeLookupTable) =>
        syntaxTree.AcceptVisitor(new ReadonlyToConstVisitor(configuration, syntaxTree));

    private sealed class ReadonlyToConstVisitor : DepthFirstAstVisitor
    {
        private readonly IDictionary<string, object> _replacements;
        private readonly SyntaxTree _syntaxTree;

        public ReadonlyToConstVisitor(IHardwareGenerationConfiguration configuration, SyntaxTree syntaxTree)
        {
            _replacements = configuration.GetOrAddReplacements();
            _syntaxTree = syntaxTree;
        }

        public override void VisitFieldDeclaration(FieldDeclaration fieldDeclaration)
        {
            base.VisitFieldDeclaration(fieldDeclaration);

            if (!fieldDeclaration.HasModifier(Modifiers.Static) ||
                !fieldDeclaration.HasModifier(Modifiers.Readonly))
            {
                return;
            }

            // We only work with field declarations that are also assignments and the value is of a primitive type, like
            // int or string.
            var initializer = fieldDeclaration
                .FindFirstChildOfType<VariableInitializer>();
            var value = initializer?.FindFirstChildOfType<PrimitiveExpression>();
            if (initializer == null || value == null) return;

            var targetAttributeName = typeof(ReplaceableAttribute).FullName;
            var replaceable = fieldDeclaration
                .Attributes
                .SelectMany(attributeSection => attributeSection.FindAllChildrenOfType<Attribute>())
                .SingleOrDefault(attribute => attribute.Type.GetFullName() == targetAttributeName);

            if (replaceable == null) return;

            var typeDeclaration = initializer.FindFirstParentOfType<TypeDeclaration>();

            var key = replaceable.FindFirstChildOfType<PrimitiveExpression>().Value.ToString();

            // If a replacement value is not set then there is nothing to do.
            if (key == null || !_replacements.TryGetValue(key, out var result)) return;

            if (result is string resultString)
            {
                result = value.Value switch
                {
                    string => resultString,
                    int => resultString.ToTechnicalInt(),
                    uint => uint.Parse(resultString, CultureInfo.InvariantCulture),
                    long => long.Parse(resultString, CultureInfo.InvariantCulture),
                    ulong => ulong.Parse(resultString, CultureInfo.InvariantCulture),
                    short => short.Parse(resultString, CultureInfo.InvariantCulture),
                    ushort => ushort.Parse(resultString, CultureInfo.InvariantCulture),
                    byte => byte.Parse(resultString, CultureInfo.InvariantCulture),
                    bool => resultString.ToUpperInvariant() == "TRUE",
                    _ => value.Value,
                };
            }

            value = new PrimitiveExpression(result);

            _syntaxTree.AcceptVisitor(new ReplaceReadonlyVisitor(initializer.Name, value.Value, typeDeclaration));
        }
    }

    private sealed class ReplaceReadonlyVisitor : DepthFirstAstVisitor
    {
        private readonly string _name;
        private readonly object _value;
        private readonly TypeDeclaration _typeDeclaration;

        public ReplaceReadonlyVisitor(string name, object value, TypeDeclaration typeDeclaration)
        {
            _name = name;
            _value = value;
            _typeDeclaration = typeDeclaration;
        }

        public override void VisitIdentifier(Identifier identifier)
        {
            base.VisitIdentifier(identifier);

            if (identifier.Parent is not MemberReferenceExpression member ||
                identifier.Name != _name ||
                !GetTypeOfStaticIdentifier(identifier).Equals(_typeDeclaration.GetActualType()))
            {
                return;
            }

            var primitive = new PrimitiveExpression(_value);
            primitive.AddAnnotation(member.GetResolveResult());
            member.ReplaceWith(primitive);
        }

        private static IType GetTypeOfStaticIdentifier(Identifier identifier) =>
            (identifier.Parent as MemberReferenceExpression)?
            .FindFirstChildOfType<TypeReferenceExpression>()?
            .Type
            .GetActualType();
    }
}
