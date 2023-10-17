using Hast.VhdlBuilder.Extensions;
using Lombiq.HelpfulLibraries.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Hast.VhdlBuilder.Representation.Declaration;

#pragma warning disable S103 // Lines should not be too long
/// <summary>
/// Represents an attribute expression containing SDC_STATEMENT that define multi-cycle paths.
/// </summary>
/// <example>
/// <para>E.g. represents the expression part of the below attribute specification.</para>
/// <code>
/// attribute altera_attribute of Imp: architecture is "-name SDC_STATEMENT ""set_multicycle_path 8 -setup -to {*PrimeCalculator::IsPrimeNumber(SimpleMemory).0._StateMachine:PrimeCalculator::IsPrimeNumber(SimpleMemory).0.binaryOperationResult.2[*]}"";-name SDC_STATEMENT ""set_multicycle_path 8 -hold  -to {*PrimeCalculator::IsPrimeNumber(SimpleMemory).0._StateMachine:PrimeCalculator::IsPrimeNumber(SimpleMemory).0.binaryOperationResult.2[*]}""";
/// </code>
/// </example>
/// <remarks>
/// <para>See <see cref="XdcFile"/> for something similar for Xilinx.</para>
/// </remarks>
[DebuggerDisplay("{ToVhdl(VhdlGenerationOptions.Debug)}")]
#pragma warning restore S103 // Lines should not be too long
public class MultiCycleSdcStatementsAttributeExpression : IVhdlElement
{
    private readonly List<SdcStatement> _paths = new();

    public void AddPath(string parentName, IDataObject pathReference, int clockCycles)
    {
        _paths.Add(new SdcStatement
        {
            ParentName = parentName,
            PathReference = pathReference,
            ClockCycles = clockCycles,
            Type = "setup",
        });

        _paths.Add(new SdcStatement
        {
            ParentName = parentName,
            PathReference = pathReference,
            ClockCycles = clockCycles - 1,
            Type = "hold",
        });
    }

    public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions) =>
        Terminated.Terminate(
            "\"" +
            string.Join(';', _paths.Select(statement => statement.ToVhdl(vhdlGenerationOptions))) +
            "\"",
            vhdlGenerationOptions);

    private sealed class SdcStatement : IVhdlElement
    {
        public string ParentName { get; set; }
        public IDataObject PathReference { get; set; }
        public int ClockCycles { get; set; }
        public string Type { get; set; }

        public string ToVhdl(IVhdlGenerationOptions vhdlGenerationOptions)
        {
            // The config should contain the path's name without backslashes even if the original name is an extended
            // identifier. Spaces need to be escaped with a slash.
            var name =
                string.IsNullOrEmpty(ParentName)
                    ? string.Empty
                    : vhdlGenerationOptions
                        .NameShortener(ParentName.TrimExtendedVhdlIdDelimiters().Replace(" ", "\\ ")) + ":";

            var vhdl = PathReference.ToVhdl(vhdlGenerationOptions).TrimExtendedVhdlIdDelimiters().Replace(" ", "\\ ");

            return StringHelper.CreateInvariant(
                $"-name SDC_STATEMENT \"\"set_multicycle_path {ClockCycles} -{Type} -to {{*{name}{vhdl}[*]}}\"\"");
        }
    }
}
