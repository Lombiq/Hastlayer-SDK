using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Semantics;
using ICSharpCode.Decompiler.TypeSystem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Services.ConstantValuesSubstitution;

internal sealed class ConstantValuesSubstitutingAstProcessor
{
    public ConstantValuesTable ConstantValuesTable { get; }
    public ITypeDeclarationLookupTable TypeDeclarationLookupTable { get; }
    public IArraySizeHolder ArraySizeHolder { get; }
    public Dictionary<string, ConstructorReference> ObjectHoldersToConstructorsMappings { get; }
    public IAstExpressionEvaluator AstExpressionEvaluator { get; }
    public IKnownTypeLookupTable KnownTypeLookupTable { get; }

    /// <summary>
    /// Gets the method invocations and object creations in the processed sub-tree, keyed by the full name of the invoked
    /// method or constructor. Rebuilt at the start of every pass.
    /// </summary>
    public ILookup<string, Expression> CallsByTargetFullName { get; private set; }

    public ConstantValuesSubstitutingAstProcessor(
        ConstantValuesTable constantValuesTable,
        ITypeDeclarationLookupTable typeDeclarationLookupTable,
        IArraySizeHolder arraySizeHolder,
        Dictionary<string, ConstructorReference> objectHoldersToConstructorsMappings,
        IAstExpressionEvaluator astExpressionEvaluator,
        IKnownTypeLookupTable knownTypeLookupTable)
    {
        ConstantValuesTable = constantValuesTable;
        TypeDeclarationLookupTable = typeDeclarationLookupTable;
        ArraySizeHolder = arraySizeHolder;
        ObjectHoldersToConstructorsMappings = objectHoldersToConstructorsMappings;
        AstExpressionEvaluator = astExpressionEvaluator;
        KnownTypeLookupTable = knownTypeLookupTable;
    }

    public void SubstituteConstantValuesInSubTree(AstNode rootNode, bool reUseOriginalConstantValuesTable)
    {
        // Gradually propagating the constant values through the syntax tree so this needs multiple passes. So running
        // them until nothing changes.

        ConstantValuesTable originalConstantValuesTable = null;
        if (reUseOriginalConstantValuesTable) originalConstantValuesTable = ConstantValuesTable.Clone();

        var constantValuesMarkingVisitor = new ConstantValuesMarkingVisitor(this, AstExpressionEvaluator);
        var objectHoldersToSubstitutedConstructorsMappingVisitor =
            new ObjectHoldersToSubstitutedConstructorsMappingVisitor(this);
        var globalValueHoldersHandlingVisitor = new GlobalValueHoldersHandlingVisitor(this, rootNode);
        var constantValuesSubstitutingVisitor = new ConstantValuesSubstitutingVisitor(this);

        string codeOutput;
        int hiddenlyUpdatedNodesUpdatedCount;
        var passCount = 0;
        const int maxPassCount = 1_000;
        do
        {
            codeOutput = rootNode.ToString();
            // Uncomment the below line to get a debug output about the current state of this sub-tree.
            ////System.IO.File.WriteAllText("ConstantSubstitution.cs", codeOutput);
            hiddenlyUpdatedNodesUpdatedCount = constantValuesMarkingVisitor.HiddenlyUpdatedNodesUpdated.Count;

            CallsByTargetFullName = CollectCalls(rootNode);

            rootNode.AcceptVisitor(constantValuesMarkingVisitor);
            rootNode.AcceptVisitor(objectHoldersToSubstitutedConstructorsMappingVisitor);
            rootNode.AcceptVisitor(globalValueHoldersHandlingVisitor);
            rootNode.AcceptVisitor(constantValuesSubstitutingVisitor);

            if (reUseOriginalConstantValuesTable) ConstantValuesTable.OverWrite(originalConstantValuesTable.Clone());
            else ConstantValuesTable.Clear();

            passCount++;
        }
        while ((codeOutput != rootNode.ToString() ||
            constantValuesMarkingVisitor.HiddenlyUpdatedNodesUpdated.Count != hiddenlyUpdatedNodesUpdatedCount) &&
                passCount < maxPassCount);

        if (passCount >= maxPassCount)
        {
            throw new InvalidOperationException(
                $"Constant substitution needs more than {maxPassCount} passes through the syntax tree starting with " +
                $"the root node {rootNode.GetFullName()}. This most possibly indicates some error or the assembly " +
                "being processed is exceptionally big.");
        }
    }

    private static ILookup<string, Expression> CollectCalls(AstNode rootNode) =>
        rootNode
            .Descendants
            .Where(node => node is InvocationExpression or ObjectCreateExpression)
            .Select(node => (
                Call: (Expression)node,
                TargetFullName: node.GetResolveResult<InvocationResolveResult>()?.Member.GetFullName()))
            .Where(call => call.TargetFullName != null)
            .ToLookup(call => call.TargetFullName, call => call.Call);

    public sealed class ConstructorReference
    {
        private string _constructorCode;

        public MethodDeclaration Constructor { get; set; }
        public Expression OriginalAssignmentTarget { get; set; }

        /// <summary>
        /// Determines whether the objects referenced by this and the other mapping were created by the same constructor
        /// with the same constant values substituted, i.e. whether their read-only members can be substituted the same
        /// way. Mappings are re-created in every pass, so reference equality alone is not enough.
        /// </summary>
        public bool IsEquivalentTo(ConstructorReference other) =>
            ReferenceEquals(this, other) ||
            (other != null &&
                Constructor.GetFullName() == other.Constructor.GetFullName() &&
                GetConstructorCode() == other.GetConstructorCode());

        private string GetConstructorCode() => _constructorCode ??= Constructor.ToString();
    }
}
