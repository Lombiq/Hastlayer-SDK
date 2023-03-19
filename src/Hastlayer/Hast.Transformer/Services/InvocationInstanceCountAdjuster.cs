using Hast.Common.Configuration;
using Hast.Layer;
using Hast.Transformer.Configuration;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;
using System;
using System.Collections.Generic;

namespace Hast.Transformer.Services;

public class InvocationInstanceCountAdjuster : IInvocationInstanceCountAdjuster
{
    private readonly ITypeDeclarationLookupTableFactory _typeDeclarationLookupTableFactory;

    public InvocationInstanceCountAdjuster(ITypeDeclarationLookupTableFactory typeDeclarationLookupTableFactory) =>
        _typeDeclarationLookupTableFactory = typeDeclarationLookupTableFactory;

    public void AdjustInvocationInstanceCounts(
        SyntaxTree syntaxTree,
        IHardwareGenerationConfiguration configuration) =>
        syntaxTree.AcceptVisitor(new InvocationInstanceCountAdjustingVisitor(
            _typeDeclarationLookupTableFactory.Create(syntaxTree),
            configuration));

    private sealed class InvocationInstanceCountAdjustingVisitor : DepthFirstAstVisitor
    {
        private readonly ITypeDeclarationLookupTable _typeDeclarationLookupTable;
        private readonly TransformerConfiguration _transformerConfiguration;
        private readonly HashSet<EntityDeclaration> _membersInvokedFromNonParallel = new();
        private readonly HashSet<EntityDeclaration> _membersInvokedFromNonRecursive = new();

        public InvocationInstanceCountAdjustingVisitor(
            ITypeDeclarationLookupTable typeDeclarationLookupTable,
            IHardwareGenerationConfiguration hardwareGenerationConfiguration)
        {
            _typeDeclarationLookupTable = typeDeclarationLookupTable;
            _transformerConfiguration = hardwareGenerationConfiguration.TransformerConfiguration();
        }

        public override void VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
        {
            base.VisitMemberReferenceExpression(memberReferenceExpression);

            if (memberReferenceExpression.FindFirstParentOfType<ICSharpCode.Decompiler.CSharp.Syntax.Attribute>() != null)
            {
                return;
            }

            AdjustInstanceCount(
                memberReferenceExpression,
                memberReferenceExpression.FindMemberDeclaration(_typeDeclarationLookupTable));
        }

        public override void VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression)
        {
            base.VisitObjectCreateExpression(objectCreateExpression);

            // Funcs are only needed for Task-based parallelism, omitting them for now.
            if (objectCreateExpression.Type.GetActualType().IsFunc())
            {
                return;
            }

            AdjustInstanceCount(
                objectCreateExpression,
                objectCreateExpression.FindConstructorDeclaration(_typeDeclarationLookupTable));
        }

        private void AdjustInstanceCount(Expression referencingExpression, EntityDeclaration referencedMember)
        {
            if (referencedMember == null) return;

            var referencedMemberMaxInvocationConfiguration = _transformerConfiguration
                .GetMaxInvocationInstanceCountConfigurationForMember(referencedMember);

            var invokingMemberMaxInvocationConfiguration = _transformerConfiguration
                .GetMaxInvocationInstanceCountConfigurationForMember(
                    referencingExpression.FindFirstParentOfType<EntityDeclaration>());

            var referencedMemberFullName = referencedMember.GetFullName();

            AdjustMaxDegreeOfParallelism(
                invokingMemberMaxInvocationConfiguration,
                referencedMemberMaxInvocationConfiguration,
                referencedMember,
                referencedMemberFullName);

            AdjustMaxRecursionDepth(
                invokingMemberMaxInvocationConfiguration,
                referencedMemberMaxInvocationConfiguration,
                referencedMember,
                referencedMemberFullName);
        }

        private void AdjustMaxDegreeOfParallelism(
            MemberInvocationInstanceCountConfiguration invoking,
            MemberInvocationInstanceCountConfiguration referenced,
            EntityDeclaration referencedMember,
            string referencedMemberFullName)
        {
            if (invoking.MaxDegreeOfParallelism > referenced.MaxDegreeOfParallelism)
            {
                referenced.MaxDegreeOfParallelism = invoking.MaxDegreeOfParallelism;

                // Was the referenced member invoked both from a parallelized member and from a non-parallelized one?
                // Then let's increase MaxDegreeOfParallelism so there is no large multiplexing logic needed, instances
                // of the referenced and invoking members can be paired.
                if (_membersInvokedFromNonParallel.Contains(referencedMember))
                {
                    referenced.MaxDegreeOfParallelism++;
                }
            }
            else if (invoking.MaxDegreeOfParallelism == 1 &&
                     !(referencedMemberFullName.IsDisplayOrClosureClassMemberName() ||
                     referencedMemberFullName.IsInlineCompilerGeneratedMethodName()))
            {
                _membersInvokedFromNonParallel.Add(referencedMember);

                // Same increment as above. Just needed so it doesn't matter whether the parallelized or the
                // non-parallelized invoking member is processed first.
                if (referenced.MaxDegreeOfParallelism != 1)
                {
                    referenced.MaxDegreeOfParallelism++;
                }
            }
        }

        private void AdjustMaxRecursionDepth(
            MemberInvocationInstanceCountConfiguration invoking,
            MemberInvocationInstanceCountConfiguration referenced,
            EntityDeclaration referencedMember,
            string referencedMemberFullName)
        {
            if (invoking.MaxRecursionDepth > referenced.MaxRecursionDepth)
            {
                referenced.MaxRecursionDepth = invoking.MaxRecursionDepth;

                // Same logic here than above with MaxDegreeOfParallelism.
                if (_membersInvokedFromNonRecursive.Contains(referencedMember))
                {
                    referenced.MaxRecursionDepth++;
                }
            }
            else if (invoking.MaxRecursionDepth == 1 &&
                     !(referencedMemberFullName.IsDisplayOrClosureClassMemberName() ||
                     referencedMemberFullName.IsInlineCompilerGeneratedMethodName()))
            {
                _membersInvokedFromNonParallel.Add(referencedMember);

                if (referenced.MaxRecursionDepth != 1)
                {
                    referenced.MaxRecursionDepth++;
                }
            }
        }
    }
}
