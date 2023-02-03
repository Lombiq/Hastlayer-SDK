using Hast.Transformer.Abstractions;
using Hast.Transformer.Vhdl.Models;
using Hast.VhdlBuilder.Representation.Declaration;
using Hast.Xilinx.Abstractions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hast.Xilinx;

public class XilinxXdcFileBuilder : XdcFileBuilderBase<XilinxDeviceManifest>
{
    public override Task<XdcFile> BuildManifestAsync(
        IEnumerable<IArchitectureComponentResult> architectureComponentResults,
        Architecture hastIpArchitecture)
    {
        // Adding multi-cycle path constraints for Vivado.

        var xdcFile = new XdcFile();

        var anyMultiCycleOperations = false;

        foreach (var architectureComponentResult in architectureComponentResults)
        {
            foreach (var operation in architectureComponentResult.ArchitectureComponent.MultiCycleOperations)
            {
                xdcFile.AddPath(
                    operation.OperationResultReference,
                    operation.RequiredClockCyclesCeiling,
                    isHierarchical: true);
                anyMultiCycleOperations = true;
            }
        }

        //// attribute dont_touch : string;
        if (anyMultiCycleOperations)
        {
            hastIpArchitecture.Declarations.Add(new LogicalBlock(
                new LineComment(
                    "When put on variables and signals this attribute instructs Vivado not to merge them, thus " +
                    "allowing us to define multi-cycle paths properly."),
                KnownDataTypes.DontTouchAttribute));
        }

        return Task.FromResult(xdcFile);
    }
}
