using Hast.Catapult.Models;
using Hast.Transformer.Vhdl.Models;
using Hast.Transformer.Vhdl.Services;
using Hast.VhdlBuilder;
using Hast.VhdlBuilder.Representation.Declaration;
using Hast.VhdlBuilder.Representation.Expression;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hast.Catapult;

public class CatapultXdcFileBuilder : XdcFileBuilderBase<CatapultDeviceManifest>
{
    public override Task<XdcFile> BuildManifestAsync(
        IEnumerable<IArchitectureComponentResult> architectureComponentResults,
        Architecture hastIpArchitecture)
    {
        // Adding multi-cycle path constraints for Quartus.

        var anyMultiCycleOperations = false;
        var sdcExpression = new MultiCycleSdcStatementsAttributeExpression();

        foreach (var architectureComponentResult in architectureComponentResults)
        {
            foreach (var operation in architectureComponentResult.ArchitectureComponent.MultiCycleOperations)
            {
                // If the path is through a global signal (i.e. that doesn't have a parent process) then the parent
                // should be empty.
                sdcExpression.AddPath(
                    operation.OperationResultReference.DataObjectKind == DataObjectKind.Variable ?
                        ProcessUtility.FindProcesses(new[] { architectureComponentResult.Body }).Single().Name :
                        string.Empty,
                    operation.OperationResultReference,
                    operation.RequiredClockCyclesCeiling);

                anyMultiCycleOperations = true;
            }
        }

        if (anyMultiCycleOperations)
        {
            var alteraAttribute = new Attribute
            {
                Name = "altera_attribute",
                ValueType = KnownDataTypes.UnrangedString,
            };

            hastIpArchitecture.Declarations.Add(new LogicalBlock(
                new LineComment(
                    "Adding multi-cycle path constraints for Quartus Prime. See: https://www.intel.com/" +
                    "content/www/us/en/programmable/support/support-resources/knowledge-base/solutions/rd05162013_635.html"),
                alteraAttribute,
                new AttributeSpecification
                {
                    Attribute = alteraAttribute,
                    Of = hastIpArchitecture.ToReference(),
                    ItemClass = "architecture",
                    Expression = sdcExpression,
                }));
        }

        return Task.FromResult<XdcFile>(null);
    }
}
