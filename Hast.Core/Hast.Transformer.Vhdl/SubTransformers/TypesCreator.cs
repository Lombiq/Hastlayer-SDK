using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Declaration;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Vhdl.SubTransformers;

public class TypesCreator : ITypesCreator
{
    private readonly IArrayTypesCreator _arrayTypesCreator;
    private readonly IEnumTypesCreator _enumTypesCreator;

    public TypesCreator(IArrayTypesCreator arrayTypesCreator, IEnumTypesCreator enumTypesCreator)
    {
        _arrayTypesCreator = arrayTypesCreator;
        _enumTypesCreator = enumTypesCreator;
    }

    public void CreateTypes(
        SyntaxTree syntaxTree,
        VhdlTransformationContext vhdlTransformationContext,
        ICollection<DependentTypesTable> dependentTypesTables,
        Architecture hastIpArchitecture)
    {
        // Adding array types for any arrays created in code. This is necessary in a separate step because in VHDL the
        // array types themselves should be created too (like in C# we'd need to first define what an int[] is before
        // being able to create one).
        var arrayTypeDependentTypes = new DependentTypesTable();
        foreach (var arrayDeclaration in _arrayTypesCreator.CreateArrayTypes(syntaxTree, vhdlTransformationContext))
        {
            arrayTypeDependentTypes.AddDependency(arrayDeclaration, arrayDeclaration.ElementType.Name);
        }

        arrayTypeDependentTypes.AddToIfNotEmpty(dependentTypesTables);

        // Adding enum types (avoid multiple enumerations).
        var enumDeclarations = _enumTypesCreator.CreateEnumTypes(syntaxTree);
        var listDeclarations = enumDeclarations is IList<IVhdlElement> list ? list : enumDeclarations.ToList();

        if (listDeclarations.Any())
        {
            var enumDeclarationsBlock = new LogicalBlock(new LineComment("Enum declarations start"));
            enumDeclarationsBlock.Body.AddRange(listDeclarations);
            enumDeclarationsBlock.Add(new LineComment("Enum declarations end"));
            hastIpArchitecture.Declarations.Add(enumDeclarationsBlock);
        }
    }
}
