using Hast.VhdlBuilder.Extensions;
using Hast.VhdlBuilder.Representation;
using Hast.VhdlBuilder.Representation.Declaration;
using Hast.VhdlBuilder.Representation.Expression;
using System.Collections.Generic;
using System.Linq;

namespace Hast.VhdlBuilder;

public static class ProcessUtility
{
    public static void AddClockToProcesses(Module module, string clockSignalName)
    {
        var clockPort = new Port
        {
            Mode = PortMode.In,
            Name = clockSignalName,
            DataType = KnownDataTypes.StdLogic,
        };

        module.Entity.Ports.Add(clockPort);

        foreach (var process in FindProcesses(module.Architecture.Body))
        {
            process.SensitivityList.Add(clockPort);
            var wrappingIf = new IfElse
            {
                Condition = new Invocation("rising_edge", clockSignalName.ToVhdlSignalReference()),
                True = new InlineBlock(process.Body.ToList()), // Needs to copy the list.
            };
            process.Body.Clear();
            process.Add(wrappingIf);
        }
    }

    public static IEnumerable<Process> FindProcesses(IEnumerable<IVhdlElement> elements) =>
        // Also looking on level down, so detecting processes even if they're in an inline block.
        elements.Where(element => element is Process)
            .Union(elements
                .Where(element => element is not Process and IBlockElement)
                .Cast<InlineBlock>()
                .SelectMany(block => block.Body.Where(element => element is Process)))
        .Cast<Process>();
}
