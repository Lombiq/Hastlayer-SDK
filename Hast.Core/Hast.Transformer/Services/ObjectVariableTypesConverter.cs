using Hast.Layer;
using Hast.Transformer.Helpers;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Services;

/// <summary>
/// Converts the type of variables of type <c>object</c> to the actual type they'll contain if this can be determined.
/// </summary>
/// <example>
/// <para>Currently the following kind of constructs are supported:</para>
/// <code>
/// // The numberObject variable will be converted to uint since apparently it is one.
/// internal bool &lt;ParallelizedArePrimeNumbers&gt;b__9_0 (object numberObject)
/// {
///     uint num;
///     num = (uint)numberObject;
///     // ...
/// }
/// </code>
/// <para>
/// Furthermore, casts to the object type corresponding to these variables when in Task starts are also removed, like
/// the one for num4 here:
/// </para>
/// <code>
/// Task.Factory.StartNew ((Func&lt;object, bool&gt;)this.&lt;ParallelizedArePrimeNumbers&gt;b__9_0, (object)num4);
/// </code>
/// </example>
/// <remarks>
/// <para>
/// This is necessary because unlike an object-typed variable in .NET that due to dynamic memory allocations can hold
/// any data in hardware the variable size should be statically determined (like fixed 32b). So compatibility with .NET
/// object variables is not complete, thus attempting to close the loop here.
/// </para>
/// </remarks>
public class ObjectVariableTypesConverter : IConverter
{
    public IEnumerable<string> Dependencies { get; } = new[] { nameof(GeneratedTaskArraysInliner) };

    public void Convert(
        SyntaxTree syntaxTree,
        IHardwareGenerationConfiguration configuration,
        IKnownTypeLookupTable knownTypeLookupTable)
    {
        syntaxTree.AcceptVisitor(new MethodObjectParametersTypeConvertingVisitor());
        syntaxTree.AcceptVisitor(new UnnecessaryObjectCastsRemovingVisitor());
    }

    private sealed class MethodObjectParametersTypeConvertingVisitor : DepthFirstAstVisitor
    {
        public override void VisitMethodDeclaration(MethodDeclaration methodDeclaration)
        {
            base.VisitMethodDeclaration(methodDeclaration);

            foreach (var objectParameter in methodDeclaration.Parameters
                .Where(parameter => parameter.Type.Is<PrimitiveType>(type =>
                    type.KnownTypeCode == KnownTypeCode.Object)))
            {
                var castExpressionFindingVisitor = new ParameterCastExpressionFindingVisitor(objectParameter.Name);
                methodDeclaration.Body.AcceptVisitor(castExpressionFindingVisitor);

                // Simply changing the parameter's type and removing the cast. Note that this will leave corresponding
                // compiler-generated Funcs intact and thus wrong. E.g. there will be similar lines added to the
                // lambda's calling method: Task.Factory.StartNew ((Func<object,
                // bool>)this.<ParallelizedArePrimeNumbers>b__9_0, num4) This will remain, despite the Func's type now
                // correctly being e.g. Func<uint, bool>. Note that the method's full name will contain the original
                // object parameter since the MemberResolveResult will still the have original parameters. This will be
                // an aesthetic issue only though: Nothing else depends on the parameters being correct here. If we'd
                // change these then the whole MemberResolveResult would need to be recreated (since parameter types, as
                // well as the list of parameters is read-only), not just here but in all the references to this method
                // too.

                var castExpression = castExpressionFindingVisitor.Expression;
                if (castExpression != null)
                {
                    var actualType = castExpression.GetActualType();

                    var resolveResult = VariableHelper
                        .CreateILVariableResolveResult(
                            ICSharpCode.Decompiler.IL.VariableKind.Parameter,
                            actualType,
                            objectParameter.Name);
                    objectParameter.Type = castExpression.Type.Clone();
                    objectParameter.RemoveAnnotations(typeof(ILVariableResolveResult));
                    objectParameter.AddAnnotation(resolveResult);
                    castExpression.ReplaceWith(castExpression.Expression);
                    castExpression.Remove();

                    methodDeclaration.Body.AcceptVisitor(new ParameterReferencesTypeChangingVisitor(objectParameter.Name, resolveResult));
                }
            }
        }

        private sealed class ParameterCastExpressionFindingVisitor : DepthFirstAstVisitor
        {
            private readonly string _parameterName;

            public CastExpression Expression { get; private set; }

            public ParameterCastExpressionFindingVisitor(string parameterName) => _parameterName = parameterName;

            public override void VisitCastExpression(CastExpression castExpression)
            {
                base.VisitCastExpression(castExpression);

                if (castExpression.Expression.Is<IdentifierExpression>(identifier => identifier.Identifier == _parameterName))
                {
                    // If there are multiple casts for the given parameter then we'll deal with it as an object unless
                    // all casts are for the same type.
                    Expression = Expression == null || Expression.Type == castExpression.Type ? castExpression : null;
                }
            }
        }

        private sealed class ParameterReferencesTypeChangingVisitor : DepthFirstAstVisitor
        {
            private readonly string _parameterName;
            private readonly ILVariableResolveResult _resolveResult;

            public ParameterReferencesTypeChangingVisitor(string parameterName, ILVariableResolveResult resolveResult)
            {
                _parameterName = parameterName;
                _resolveResult = resolveResult;
            }

            public override void VisitIdentifierExpression(IdentifierExpression identifierExpression)
            {
                base.VisitIdentifierExpression(identifierExpression);

                if (identifierExpression.Identifier != _parameterName) return;

                identifierExpression.ReplaceAnnotations(_resolveResult);
            }
        }
    }

    private sealed class UnnecessaryObjectCastsRemovingVisitor : DepthFirstAstVisitor
    {
        public override void VisitCastExpression(CastExpression castExpression)
        {
            base.VisitCastExpression(castExpression);

            if (castExpression.GetActualType().FullName != typeof(object).FullName ||
                !castExpression.Parent.Is<InvocationExpression>(invocation => invocation.IsTaskStart()))
            {
                return;
            }

            castExpression.ReplaceWith(castExpression.Expression.Detach());
        }
    }
}
