using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace Hast.Transformer.Services.ConstantValuesSubstitution;

internal sealed class ConstantValuesSubstitutingAstProcessor
{
    public ConstantValuesTable ConstantValuesTable { get; }
    public ITypeDeclarationLookupTable TypeDeclarationLookupTable { get; }
    public IArraySizeHolder ArraySizeHolder { get; }
    public Dictionary<string, ConstructorReference> ObjectHoldersToConstructorsMappings { get; }
    public IAstExpressionEvaluator AstExpressionEvaluator { get; }
    public IKnownTypeLookupTable KnownTypeLookupTable { get; }

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

        if (passCount == maxPassCount)
        {
            throw new InvalidOperationException(
                "Constant substitution needs more than " + maxPassCount +
                "passes through the syntax tree starting with the root node " + rootNode.GetFullName() +
                ". This most possibly indicates some error or the assembly being processed is exceptionally big.");
        }
    }

    public sealed class ConstructorReference
    {
        public MethodDeclaration Constructor { get; set; }
        public Expression OriginalAssignmentTarget { get; set; }
    }
}
