using Hast.Layer;
using Hast.Transformer.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;

namespace Hast.Transformer.Services.ConstantValuesSubstitution;

public class ConstantValuesSubstitutor : IConstantValuesSubstitutor
{
    private readonly ITypeDeclarationLookupTableFactory _typeDeclarationLookupTableFactory;
    private readonly IAstExpressionEvaluator _astExpressionEvaluator;

    public ConstantValuesSubstitutor(
        ITypeDeclarationLookupTableFactory typeDeclarationLookupTableFactory,
        IAstExpressionEvaluator astExpressionEvaluator)
    {
        _typeDeclarationLookupTableFactory = typeDeclarationLookupTableFactory;
        _astExpressionEvaluator = astExpressionEvaluator;
    }

    public void SubstituteConstantValues(
        SyntaxTree syntaxTree,
        IArraySizeHolder arraySizeHolder,
        IHardwareGenerationConfiguration configuration,
        IKnownTypeLookupTable knownTypeLookupTable) => new ConstantValuesSubstitutingAstProcessor(
            new ConstantValuesTable(),
            _typeDeclarationLookupTableFactory.Create(syntaxTree),
            arraySizeHolder,
            [],
            _astExpressionEvaluator,
            knownTypeLookupTable)
            .SubstituteConstantValuesInSubTree(syntaxTree, reUseOriginalConstantValuesTable: false);
}
