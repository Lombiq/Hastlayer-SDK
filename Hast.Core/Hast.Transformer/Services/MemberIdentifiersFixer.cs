using Hast.Layer;
using Hast.Transformer.Helpers;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Resolver;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;
using System;
using System.Linq;

namespace Hast.Transformer.Services;

/// <summary>
/// If a method is called from within its own class then the target of the <see cref="InvocationExpression"/> will be an
/// <see cref="IdentifierExpression"/>. However, it should really be a <see cref="MemberReferenceExpression"/> as it was
/// in ILSpy prior to v3 and as it is if it's called from another class. See: <see
/// href="https://github.com/icsharpcode/ILSpy/issues/1407"/>.
/// </summary>
public class MemberIdentifiersFixer : IConverter
{
    public void Convert(
        SyntaxTree syntaxTree,
        IHardwareGenerationConfiguration configuration,
        IKnownTypeLookupTable knownTypeLookupTable) =>
        syntaxTree.AcceptVisitor(new MemberIdentifiersFixingVisitor());

    private sealed class MemberIdentifiersFixingVisitor : DepthFirstAstVisitor
    {
        public override void VisitIdentifierExpression(IdentifierExpression identifierExpression)
        {
            base.VisitIdentifierExpression(identifierExpression);

            var parent = identifierExpression.Parent;
            var identifier = identifierExpression.Identifier;

            IMember member;
            if (parent is InvocationExpression invocation && invocation.Target == identifierExpression)
            {
                // A normal method invocation.
                member = parent.GetMemberResolveResult()?.Member;
            }
            else if (identifierExpression
                .GetResolveResult<MethodGroupResolveResult>()
                ?.Methods
                ?.Any(method => method.GetFullName().IsInlineCompilerGeneratedMethodName()) == true)
            {
                // A reference to a DisplayClass member or compiler-generated method within a Task.Factory.StartNew
                // call.
                member = identifierExpression.GetResolveResult<MethodGroupResolveResult>()?.Methods.Single();
            }
            else if (identifierExpression.GetMemberResolveResult() is MemberResolveResult memberResolveResult)
            {
                // A property access.
                if (memberResolveResult.Member.Name != identifier)
                {
                    return;
                }

                member = memberResolveResult.Member;
            }
            else
            {
                return;
            }

            if (member == null) return;

            if (member.IsStatic)
            {
                var typeResolveResult = new TypeResolveResult(member.DeclaringType);
                var typeReferenceExpression =
                    new TypeReferenceExpression(TypeHelper.CreateAstType(member.DeclaringType))
                    .WithAnnotation(typeResolveResult);
                var memberReference = new MemberReferenceExpression(typeReferenceExpression, identifier)
                    .WithAnnotation(new MemberResolveResult(typeResolveResult, member));
                identifierExpression.ReplaceWith(memberReference);
            }
            else
            {
                var thisResolveResult = new ThisResolveResult(member.DeclaringType);
                var thisReferenceExpression = new ThisReferenceExpression().WithAnnotation(thisResolveResult);

                var memberReference = new MemberReferenceExpression(thisReferenceExpression, identifier)
                    .WithAnnotation(new MemberResolveResult(thisResolveResult, member));
                identifierExpression.ReplaceWith(memberReference);
            }
        }
    }
}
