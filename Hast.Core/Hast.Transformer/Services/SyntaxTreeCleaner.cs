using Hast.Common.Extensions;
using Hast.Layer;
using Hast.Transformer.Configuration;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Services;

public class SyntaxTreeCleaner : ISyntaxTreeCleaner, IConverter
{
    public IEnumerable<string> Dependencies { get; } = new[] { nameof(FSharpIdiosyncrasiesAdjuster) };

    private readonly ITypeDeclarationLookupTableFactory _typeDeclarationLookupTableFactory;
    private readonly IMemberSuitabilityChecker _memberSuitabilityChecker;

    public SyntaxTreeCleaner(
        ITypeDeclarationLookupTableFactory typeDeclarationLookupTableFactory,
        IMemberSuitabilityChecker memberSuitabilityChecker)
    {
        _typeDeclarationLookupTableFactory = typeDeclarationLookupTableFactory;
        _memberSuitabilityChecker = memberSuitabilityChecker;
    }

    public void Convert(
        SyntaxTree syntaxTree,
        IHardwareGenerationConfiguration configuration,
        IKnownTypeLookupTable knownTypeLookupTable) =>
        CleanUnusedDeclarations(syntaxTree, configuration);

    public void CleanUnusedDeclarations(SyntaxTree syntaxTree, IHardwareGenerationConfiguration configuration)
    {
        var typeDeclarationLookupTable = _typeDeclarationLookupTableFactory.Create(syntaxTree);
        var noIncludedMembers = !configuration.HardwareEntryPointMemberFullNames.Any() &&
            !configuration.HardwareEntryPointMemberNamePrefixes.Any();
        var referencedNodesFlaggingVisitor = new ReferencedNodesFlaggingVisitor(typeDeclarationLookupTable, configuration);

        // Starting with hardware entry point members we walk through the references to see which declarations are used
        // (e.g. which methods are called at least once).
        foreach (var member in syntaxTree.GetAllTypeDeclarations().SelectMany(type => type.Members))
        {
            var fullName = member.GetFullName();
            if (!(noIncludedMembers ||
                    configuration.HardwareEntryPointMemberFullNames.Contains(fullName) ||
                    fullName.GetMemberNameAlternates().Intersect(configuration.HardwareEntryPointMemberFullNames).Any() ||
                    configuration.HardwareEntryPointMemberNamePrefixes
                        .Any(prefix => member.GetSimpleName().StartsWithOrdinal(prefix))) ||
                !_memberSuitabilityChecker.IsSuitableHardwareEntryPointMember(member, typeDeclarationLookupTable))
            {
                continue;
            }

            if (member is MethodDeclaration declaration &&
                declaration.FindImplementedInterfaceMethod(typeDeclarationLookupTable.Lookup)
                    is { } implementedInterfaceMethod)
            {
                implementedInterfaceMethod.AddReference(member);
                implementedInterfaceMethod.FindFirstParentTypeDeclaration().AddReference(member);
            }

            member.SetHardwareEntryPointMember();

            ReferenceAllParentTypes(syntaxTree, member);

            member.AcceptVisitor(referencedNodesFlaggingVisitor);
        }

        // Then removing all unused declarations.
        syntaxTree.AcceptVisitor(new UnreferencedNodesRemovingVisitor());

        // Removing orphaned base types.
        foreach (var baseTypes in syntaxTree.GetAllTypeDeclarations().Select(type => type.BaseTypes))
        {
            foreach (var baseType in baseTypes.Where(baseType => !typeDeclarationLookupTable.Lookup(baseType).IsReferenced()))
            {
                baseTypes.Remove(baseType);
            }
        }

        // Cleaning up empty namespaces.
        var emptyNamespaceDeclarations = syntaxTree
            .Members
            .Where(member =>
                member is NamespaceDeclaration namespaceDeclaration &&
                !namespaceDeclaration.Members.Any());
        foreach (var namespaceDeclaration in emptyNamespaceDeclarations) namespaceDeclaration.Remove();

        // Note that at this point the reference counters are out of date and would need to be refreshed to be used.

        syntaxTree.AcceptVisitor(new ReferenceMetadataCleanUpVisitor());
    }

    private static void ReferenceAllParentTypes(SyntaxTree syntaxTree, EntityDeclaration member)
    {
        // Referencing all parent types. This is necessary if the hardware entry point is in a nested class.
        var parent = member.FindFirstParentTypeDeclaration();
        var child = member;
        while (parent != null)
        {
            child.AddReference(parent);
            child = parent;
            parent = parent.FindFirstParentTypeDeclaration();
        }

        child.AddReference(syntaxTree);
    }

    private sealed class ReferencedNodesFlaggingVisitor : DepthFirstAstVisitor
    {
        private readonly ITypeDeclarationLookupTable _typeDeclarationLookupTable;
        private readonly IHardwareGenerationConfiguration _configuration;

        public ReferencedNodesFlaggingVisitor(
            ITypeDeclarationLookupTable typeDeclarationLookupTable,
            IHardwareGenerationConfiguration configuration)
        {
            _typeDeclarationLookupTable = typeDeclarationLookupTable;
            _configuration = configuration;
        }

        public override void VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression)
        {
            base.VisitObjectCreateExpression(objectCreateExpression);

            var instantiatedType = _typeDeclarationLookupTable.Lookup(objectCreateExpression.Type);

            if (instantiatedType == null) return;

            var constructorFullName = objectCreateExpression.GetConstructorFullName();

            instantiatedType.AddReference(objectCreateExpression);

            if (!string.IsNullOrEmpty(constructorFullName))
            {
                // Looking up the constructor used.
                var constructor = instantiatedType.Members
                    .SingleOrDefault(member => member.GetFullName() == constructorFullName);
                if (constructor != null)
                {
                    constructor.AddReference(objectCreateExpression);
                    constructor.AcceptVisitor(this);
                }
            }

            // Also marking those members referenced that are used in an object initializer.
            if (objectCreateExpression.Initializer.Elements.Any())
            {
                foreach (var element in objectCreateExpression.Initializer.Elements)
                {
                    instantiatedType.Members
                        .SingleOrDefault(member => member.GetFullName() == element.GetFullName())
                        .AddReference(objectCreateExpression);
                }
            }
        }

        public override void VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression)
        {
            base.VisitDefaultValueExpression(defaultValueExpression);

            var defaultedType = _typeDeclarationLookupTable.Lookup(defaultValueExpression.Type);

            if (defaultedType == null) return;

            defaultedType.AddReference(defaultValueExpression);
        }

        public override void VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression)
        {
            base.VisitMemberReferenceExpression(memberReferenceExpression);

            if (memberReferenceExpression.Target is TypeReferenceExpression typeReferenceExpression &&
                typeReferenceExpression.Type.Is<SimpleType>(simple => simple.Identifier == "MethodImplOptions"))
            {
                // This can happen when a method is extern (see:
                // https://msdn.microsoft.com/en-us/library/e59b22c5.aspx), thus has no body but has the MethodImpl
                // attribute (e.g. Math.Abs(double value). Nothing to do.
                return;
            }

            var member = memberReferenceExpression.FindMemberDeclaration(_typeDeclarationLookupTable);

            if (member == null || member.WasVisited()) return;

            // Using the reference expression as the "from", since e.g. two calls to the same method should be counted
            // twice, even if from the same method.
            member.AddReference(memberReferenceExpression);

            // Referencing the member's parent as well.
            member.FindFirstParentTypeDeclaration().AddReference(memberReferenceExpression);

            // And also the interfaces implemented by it.
            if (_configuration.TransformerConfiguration().ProcessImplementedInterfaces &&
                (member is MethodDeclaration || member is PropertyDeclaration))
            {
                var implementedInterfaceMethod = member.FindImplementedInterfaceMethod(_typeDeclarationLookupTable.Lookup);
                if (implementedInterfaceMethod != null)
                {
                    implementedInterfaceMethod.AddReference(member);
                    implementedInterfaceMethod.FindFirstParentTypeDeclaration().AddReference(member);
                }
            }

            member.SetVisited();

            // Since when e.g. another method is referenced that is above the level of this expression in the syntax
            // tree, thus it won't be visited unless we start a visitor there too.
            member.AcceptVisitor(this);
        }
    }

    private sealed class UnreferencedNodesRemovingVisitor : DepthFirstAstVisitor
    {
        public override void VisitCustomEventDeclaration(CustomEventDeclaration eventDeclaration)
        {
            base.VisitCustomEventDeclaration(eventDeclaration);
            RemoveIfUnreferenced(eventDeclaration);
        }

        public override void VisitEventDeclaration(EventDeclaration eventDeclaration)
        {
            base.VisitEventDeclaration(eventDeclaration);
            RemoveIfUnreferenced(eventDeclaration);
        }

        public override void VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration)
        {
            base.VisitDelegateDeclaration(delegateDeclaration);
            RemoveIfUnreferenced(delegateDeclaration);
        }

        public override void VisitExternAliasDeclaration(ExternAliasDeclaration externAliasDeclaration)
        {
            base.VisitExternAliasDeclaration(externAliasDeclaration);
            RemoveIfUnreferenced(externAliasDeclaration);
        }

        public override void VisitTypeDeclaration(TypeDeclaration typeDeclaration)
        {
            base.VisitTypeDeclaration(typeDeclaration);

            var unreferencedMembers = typeDeclaration.Members.Where(member => !member.IsReferenced()).ToList();

            // Removing the type if it's empty but leaving if it was referenced as a type (which is with an object
            // create expression or a default value expression).
            if (typeDeclaration.Members.Count == unreferencedMembers.Count && !typeDeclaration.IsReferenced())
            {
                typeDeclaration.Remove();
            }

            foreach (var member in unreferencedMembers)
            {
                member.Remove();
            }
        }

        private static void RemoveIfUnreferenced(AstNode node)
        {
            if (!node.IsReferenced()) node.Remove();
        }
    }

    private sealed class ReferenceMetadataCleanUpVisitor : DepthFirstAstVisitor
    {
        protected override void VisitChildren(AstNode node)
        {
            base.VisitChildren(node);

            node.RemoveReferenceMetadata();
        }
    }
}
