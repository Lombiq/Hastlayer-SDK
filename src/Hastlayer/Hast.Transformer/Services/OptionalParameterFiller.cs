using Hast.Layer;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Services;

/// <summary>
/// Arguments for optional method parameters can be omitted. This service fills those too so later processing these
/// method calls can be easier. Note that method class include constructors as well.
/// </summary>
/// <remarks>
/// <para>
/// Needs to run before method inlining but after anything that otherwise modified method signatures or invocations.
/// </para>
/// </remarks>
/// <example>
/// <para>The constructor's signature being:</para>
/// <code>
/// public BitMask(ushort size, bool allOne = false)
/// </code>
///
/// <para>...the following code:</para>
/// <code>
/// var resultFractionBits = new BitMask(left._environment.Size);
/// </code>
///
/// <para>...will be changed into:</para>
/// <code>
/// var resultFractionBits = new BitMask(left._environment.Size, false);
/// </code>
/// </example>
public class OptionalParameterFiller : IConverter
{
    public IEnumerable<string> Dependencies { get; } = new[] { nameof(EmbeddedAssignmentExpressionsExpander) };
    private readonly ITypeDeclarationLookupTableFactory _typeDeclarationLookupTableFactory;

    public OptionalParameterFiller(ITypeDeclarationLookupTableFactory typeDeclarationLookupTableFactory) =>
        _typeDeclarationLookupTableFactory = typeDeclarationLookupTableFactory;

    public void Convert(
        SyntaxTree syntaxTree,
        IHardwareGenerationConfiguration configuration,
        IKnownTypeLookupTable knownTypeLookupTable) =>
        syntaxTree.AcceptVisitor(new OptionalParamtersFillingVisitor(_typeDeclarationLookupTableFactory.Create(syntaxTree)));

    private sealed class OptionalParamtersFillingVisitor : DepthFirstAstVisitor
    {
        private readonly ITypeDeclarationLookupTable _typeDeclarationLookupTable;

        public OptionalParamtersFillingVisitor(ITypeDeclarationLookupTable typeDeclarationLookupTable) =>
            _typeDeclarationLookupTable = typeDeclarationLookupTable;

        public override void VisitInvocationExpression(InvocationExpression invocationExpression)
        {
            base.VisitInvocationExpression(invocationExpression);

            if (invocationExpression.Target is not MemberReferenceExpression memberReferenceExpression ||
                !memberReferenceExpression.IsMethodReference())
            {
                return;
            }

            var method = (MethodDeclaration)memberReferenceExpression.FindMemberDeclaration(_typeDeclarationLookupTable);

            if (method == null) return;

            FillOptionalParameters(invocationExpression.Arguments, method.Parameters);
        }

        public override void VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression)
        {
            base.VisitObjectCreateExpression(objectCreateExpression);

            if (objectCreateExpression.FindConstructorDeclaration(_typeDeclarationLookupTable) is not MethodDeclaration constructor) return;

            // Need to skip the first "this" parameter.
            FillOptionalParameters(objectCreateExpression.Arguments, constructor.Parameters.Skip(1).ToList());
        }

        private static void FillOptionalParameters(
            ICollection<Expression> arguments,
            ICollection<ParameterDeclaration> parameters)
        {
            if (arguments.Count == parameters.Count) return;

            // All the remaining parameters are optional ones.
            var parametersArray = parameters.AsList();
            for (int i = arguments.Count; i < parameters.Count; i++)
            {
                arguments.Add(parametersArray[i].DefaultExpression.Clone());
            }
        }
    }
}
