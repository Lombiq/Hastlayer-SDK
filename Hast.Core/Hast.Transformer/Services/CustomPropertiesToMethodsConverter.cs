using Hast.Layer;
using Hast.Transformer.Helpers;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Services;

/// <summary>
/// Converts custom implemented properties' setters and getters into equivalent methods so it's easier to transform
/// them.
/// </summary>
/// <example>
/// <code>
/// public uint NumberPlusFive
/// {
///     get { return Number + 5; }
///     set { Number = value - 5; }
/// }
/// </code>
/// <para>The above property will be converted as below.</para>
/// <code>
/// uint uint get_NumberPlusFive()
/// {
///     return Number + 5u;
/// }
///
/// void void set_NumberPlusFive(uint value)
/// {
///     Number = value - 5u;
/// }
/// </code>
/// </example>
public class CustomPropertiesToMethodsConverter : IConverter
{
    public IEnumerable<string> Dependencies { get; } = new[] { nameof(OperatorsToMethodsConverter) };

    public void Convert(
        SyntaxTree syntaxTree,
        IHardwareGenerationConfiguration configuration,
        IKnownTypeLookupTable knownTypeLookupTable) =>
        syntaxTree.AcceptVisitor(new CustomPropertiesConvertingVisitor(syntaxTree));

    private sealed class CustomPropertiesConvertingVisitor : DepthFirstAstVisitor
    {
        private readonly SyntaxTree _syntaxTree;

        public CustomPropertiesConvertingVisitor(SyntaxTree syntaxTree) => _syntaxTree = syntaxTree;

        public override void VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration)
        {
            base.VisitPropertyDeclaration(propertyDeclaration);

            // We only care about properties with custom implemented getters and/or setters. If the getter and the
            // setter is empty then it's an auto-property. If the getter is compiler-generated then it's also an
            // auto-property (a read-only one).
            if ((!propertyDeclaration.Getter.Body.Any() && !propertyDeclaration.Setter.Body.Any()) ||
                (propertyDeclaration.GetMemberResolveResult().Member as IProperty).Getter.IsCompilerGenerated())
            {
                return;
            }

            var parentType = propertyDeclaration.FindFirstParentTypeDeclaration();

            var getter = propertyDeclaration.Getter;
            if (getter.Body.Any())
            {
                var getterMethod = MethodDeclarationFactory.CreateMethod(
                    name: getter.GetMemberResolveResult().Member.Name,
                    annotations: getter.Annotations,
                    attributes: getter.Attributes,
                    parameters: Enumerable.Empty<ParameterDeclaration>(),
                    body: getter.Body,
                    returnType: propertyDeclaration.ReturnType);
                parentType.Members.Add(getterMethod);
            }

            var setter = propertyDeclaration.Setter;
            if (setter.Body.Any())
            {
                var valueParameter = new ParameterDeclaration(propertyDeclaration.ReturnType.Clone(), "value")
                    .WithAnnotation(VariableHelper.CreateILVariableResolveResult(VariableKind.Parameter, getter.GetActualType(), "value"));
                var setterMethod = MethodDeclarationFactory.CreateMethod(
                    name: setter.GetMemberResolveResult().Member.Name,
                    annotations: setter.Annotations,
                    attributes: setter.Attributes,
                    parameters: new[] { valueParameter },
                    body: setter.Body,
                    returnType: new ICSharpCode.Decompiler.CSharp.Syntax.PrimitiveType("void"));
                parentType.Members.Add(setterMethod);
            }

            // Changing consumer code of the property to use it as methods.
            _syntaxTree.AcceptVisitor(new PropertyAccessChangingVisitor(
                getter.Body.Any() ? getter.GetFullName() : null,
                setter.Body.Any() ? setter.GetFullName() : null));

            propertyDeclaration.Remove();
        }

        private sealed class PropertyAccessChangingVisitor : DepthFirstAstVisitor
        {
            private readonly string _getterName;
            private readonly string _setterName;

            public PropertyAccessChangingVisitor(string getterName, string setterName)
            {
                _getterName = getterName;
                _setterName = setterName;
            }

            public override void VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
            {
                base.VisitMemberReferenceExpression(memberReferenceExpression);

                var memberFullName = memberReferenceExpression.GetMemberFullName();
                var memberResolveResult = memberReferenceExpression.GetMemberResolveResult();
                if (memberResolveResult?.Member is not IProperty property) return;

                if (memberFullName == _getterName)
                {
                    memberReferenceExpression.MemberName = property.Getter.Name;
                    var invocation = new InvocationExpression(memberReferenceExpression.Clone());
                    invocation.AddAnnotation(new InvocationResolveResult(memberResolveResult.TargetResult, property.Getter));
                    memberReferenceExpression.ReplaceWith(invocation);
                }

                // The parent of a property setter should be an assignment normally but in attributes it won't be.
                else if (memberFullName == _setterName &&
                    memberReferenceExpression.Parent is AssignmentExpression assignmentExpression)
                {
                    // Note that the MemberName change needs to happen before creating the InvocationExpression.
                    memberReferenceExpression.MemberName = property.Setter.Name;
                    var invocation = new InvocationExpression(memberReferenceExpression.Clone());
                    invocation.AddAnnotation(new InvocationResolveResult(memberResolveResult.TargetResult, property.Setter));
                    invocation.Arguments.Add(assignmentExpression.Right.Clone());
                    assignmentExpression.ReplaceWith(invocation);
                }
            }
        }
    }
}
