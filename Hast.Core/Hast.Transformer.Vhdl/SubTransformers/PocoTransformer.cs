using Hast.Transformer.Vhdl.ArchitectureComponents;
using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Representation.Declaration;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hast.Transformer.Vhdl.SubTransformers;

public class PocoTransformer : IPocoTransformer
{
    private readonly IRecordComposer _recordComposer;
    private readonly IDisplayClassFieldTransformer _displayClassFieldTransformer;

    public PocoTransformer(IRecordComposer recordComposer, IDisplayClassFieldTransformer displayClassFieldTransformer)
    {
        _recordComposer = recordComposer;
        _displayClassFieldTransformer = displayClassFieldTransformer;
    }

    public bool IsSupported(AstNode node) =>
        _recordComposer.IsSupported(node) ||
        (node is FieldDeclaration declaration && !_displayClassFieldTransformer.IsDisplayClassField(declaration));

    public Task<IMemberTransformerResult> TransformAsync(TypeDeclaration typeDeclaration, IVhdlTransformationContext context) =>
        Task.Run<IMemberTransformerResult>(() =>
        {
            var result = new MemberTransformerResult
            {
                Member = typeDeclaration,
            };

            var record = _recordComposer.CreateRecordFromType(typeDeclaration, context);
            var component = new BasicComponent(record.Name);

            if (record.Fields.Any())
            {
                var hasDependency = false;

                foreach (var dataType in record.Fields.Select(field => field.DataType).Where(dataType => dataType is ArrayTypeBase or Record))
                {
                    component.DependentTypesTable.AddDependency(record, dataType.Name);
                    hasDependency = true;
                }

                if (!hasDependency) component.DependentTypesTable.AddBaseType(record);
            }

            result.ArchitectureComponentResults = new List<IArchitectureComponentResult>
            {
                new ArchitectureComponentResult(component),
            };

            return result;
        });
}
