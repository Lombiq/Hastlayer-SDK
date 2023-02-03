using CsvHelper;
using CsvHelper.Configuration;
using Hast.Synthesis.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using Lombiq.HelpfulLibraries.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Hast.Synthesis.Services;

public class TimingReportParser : ITimingReportParser
{
    private static readonly CsvConfiguration CsvConfiguration = new(CultureInfo.InvariantCulture)
    {
        MissingFieldFound = null, // Tolerate missing fields.
    };

    public ITimingReport Parse(TextReader reportReader)
    {
        using var csvReader = new CsvReader(reportReader, CsvConfiguration);

        csvReader.Configuration.Delimiter = "\t";
        csvReader.Configuration.CultureInfo = CultureInfo.InvariantCulture;

        csvReader.Read();
        csvReader.ReadHeader();

        var timingReport = new TimingReport();

        while (csvReader.Read())
        {
            var operatorString = csvReader.GetField<string>("Op");

            // These are not directly applicable in .NET.
            var skippedOperators = new[] { "nand", "nor", "xnor" };
            if (skippedOperators.Contains(operatorString))
            {
                continue;
            }

            // Instead of the shift_left/right* versions we use dotnet_shift_left/right, which also takes a surrounding
            // SmartResize() call into account.
            if (operatorString.StartsWithOrdinal("shift_left") ||
                operatorString.StartsWithOrdinal("shift_right"))
            {
                continue;
            }

            var dpdString = csvReader.GetField<string>("DPD");

            // If the DPD is not specified then nothing to do.
            if (string.IsNullOrEmpty(dpdString))
            {
                continue;
            }

            // Operators can be simple ones (like and and add) or ones that can also take a constant operand (like div).
            // This is so that if one operand is a const that's a power of two we have a different timing value,
            // addressing specific VHDL compiler optimizations (like with div_by_4).

            var constantOperand = operatorString.Partition("_by_") is (_, "_by_", var constantOperandString)
                ? constantOperandString
                : string.Empty;

            var operandType = csvReader.GetField<string>("InType");
            var isSigned = operandType.StartsWithOrdinal("signed");
            var operandSizeMatch = operandType.RegexMatch("([0-9]+)", RegexOptions.Compiled);
            if (!operandSizeMatch.Success)
            {
                throw new InvalidOperationException("The \"" + operandType + "\" operand type doesn't have a size.");
            }

            var operandSizeBits = ushort.Parse(operandSizeMatch.Groups[1].Value, CultureInfo.InvariantCulture);

            var isSignAgnosticBinaryOperatorType = false;
            var isSignAgnosticUnaryOperatorType = false;

            var (binaryOperator, unaryOperator) = ParseOperator(
                operatorString,
                operandSizeBits,
                ref isSignAgnosticBinaryOperatorType,
                ref isSignAgnosticUnaryOperatorType);

            // For more info on DPD and TWDFR see the docs of Hastlayer Timing Tester. Data Path Delay, i.e. the
            // propagation of signals through the operation and the nets around it.
            var dpd = decimal.Parse(
                dpdString.Replace(',', '.'), // Taking care of decimal commas.
                NumberStyles.Any,
                CultureInfo.InvariantCulture);

            // Timing window difference from requirement, i.e.:
            // For Vivado:
            // TWDFR = Requirement plus delays - Source clock delay - Requirement for arrival (clock period)
            // For Quartus Prime:
            // TWDFR = Data required time -(Data Arrival Time -Data Delay) -Setup relationship(clock period)
            // This is currently not used, but all timing reports contain it and may be required in the future.
            ////var twdfr = decimal.Parse(
            ////    csvReader.GetField<string>("TWDFR").Replace(',', '.'), // Taking care of decimal commas.
            ////    NumberStyles.Any,
            ////    CultureInfo.InvariantCulture);

            var operatorInfo = new OperatorInfo
            {
                OperandSizeBits = operandSizeBits,
                IsSigned = isSigned,
                ConstantOperand = constantOperand,
                Dpd = dpd,
            };
            var binaryOperatorHasvalue = WhenBinaryOperatorHasvalue(
                timingReport,
                binaryOperator,
                isSignAgnosticBinaryOperatorType,
                operatorInfo);
            if (!binaryOperatorHasvalue)
            {
                WhenUnaryOperatorHasvalue(
                    timingReport,
                    unaryOperator,
                    isSignAgnosticUnaryOperatorType,
                    operatorInfo);
            }
        }

        return timingReport;
    }

    private static (BinaryOperatorType? BinaryOperator, UnaryOperatorType? UnaryOperator) ParseOperator(
        string operatorString,
        int operandSizeBits,
        ref bool isSignAgnosticBinaryOperatorType,
        ref bool isSignAgnosticUnaryOperatorType)
    {
        isSignAgnosticBinaryOperatorType =
            isSignAgnosticBinaryOperatorType ||
            operatorString is "and" or "or" or "xor";
        isSignAgnosticUnaryOperatorType = isSignAgnosticUnaryOperatorType || operatorString == "not";

        // Notes:
        // xor:

        var binaryOrUnaryOperator = operatorString switch
        {
            "and" => BinaryOperatorType.BitwiseAnd,
            "add" => BinaryOperatorType.Add,
            var op when op.StartsWithOrdinal("div") => BinaryOperatorType.Divide,
            "eq" => BinaryOperatorType.Equality,
            "ge" => BinaryOperatorType.GreaterThanOrEqual,
            "gt" => BinaryOperatorType.GreaterThan,
            "le" => BinaryOperatorType.LessThanOrEqual,
            "lt" => BinaryOperatorType.LessThan,
            // BinaryOperatorType.Modulus is actually the remainder operator and corresponds to the VHDL operator rem,
            // see below.
            "mod" => Union.Neither<BinaryOperatorType?, UnaryOperatorType?>(),
            var op when op.StartsWithOrdinal("mul") => BinaryOperatorType.Multiply,
            "neq" => BinaryOperatorType.InEquality,
            "not" => operandSizeBits == 1 ? UnaryOperatorType.Not : UnaryOperatorType.BitNot,
            "or" => BinaryOperatorType.BitwiseOr,
            var op when op.StartsWithOrdinal("dotnet_shift_left") =>
                BinaryOperatorType.ShiftLeft,
            var op when op.StartsWithOrdinal("dotnet_shift_right") => BinaryOperatorType
                .ShiftRight,
            "rem" => BinaryOperatorType.Modulus,
            "sub" => BinaryOperatorType.Subtract,
            "unary_minus" => UnaryOperatorType.Minus,
            // There is no separate bitwise and conditional version for XOR.
            "xor" => BinaryOperatorType.ExclusiveOr,
            _ => throw new NotSupportedException("Unrecognized operator in timing report: " + operatorString + "."),
        };

        return binaryOrUnaryOperator.ToTuple();
    }

    private static bool WhenBinaryOperatorHasvalue(
        TimingReport timingReport,
        BinaryOperatorType? binaryOperator,
        bool isSignAgnosticBinaryOperatorType,
        OperatorInfo operatorInfo)
    {
        if (!binaryOperator.HasValue) return false;

        (ushort operandSizeBits, bool isSigned, string constantOperand, decimal dpd) = operatorInfo;

        timingReport.SetLatencyNs(binaryOperator.Value, operandSizeBits, isSigned, constantOperand, dpd);

        if (isSignAgnosticBinaryOperatorType)
        {
            timingReport.SetLatencyNs(binaryOperator.Value, operandSizeBits, !isSigned, constantOperand, dpd);
        }

        // Bitwise and/or are defined for bools too, so need to handle that above, then handling their conditional pairs
        // here. (Unary bit not is only defined for arithmetic types.)
        if (operandSizeBits == 1 &&
            (binaryOperator == BinaryOperatorType.BitwiseOr || binaryOperator == BinaryOperatorType.BitwiseAnd))
        {
            binaryOperator = binaryOperator == BinaryOperatorType.BitwiseOr ?
                BinaryOperatorType.ConditionalOr : BinaryOperatorType.ConditionalAnd;

            timingReport.SetLatencyNs(binaryOperator.Value, operandSizeBits, isSigned, constantOperand, dpd);

            if (isSignAgnosticBinaryOperatorType)
            {
                timingReport.SetLatencyNs(binaryOperator.Value, operandSizeBits, !isSigned, constantOperand, dpd);
            }
        }

        return true;
    }

    private static void WhenUnaryOperatorHasvalue(
        TimingReport timingReport,
        UnaryOperatorType? unaryOperator,
        bool isSignAgnosticUnaryOperatorType,
        OperatorInfo operatorInfo)
    {
        if (!unaryOperator.HasValue) return;

        (ushort operandSizeBits, bool isSigned, string constantOperand, decimal dpd) = operatorInfo;

        timingReport.SetLatencyNs(unaryOperator.Value, operandSizeBits, isSigned, constantOperand, dpd);

        if (isSignAgnosticUnaryOperatorType)
        {
            timingReport.SetLatencyNs(unaryOperator.Value, operandSizeBits, !isSigned, constantOperand, dpd);
        }
    }

    private sealed class OperatorInfo
    {
        public ushort OperandSizeBits { get; set; }
        public bool IsSigned { get; set; }
        public string ConstantOperand { get; set; }
        public decimal Dpd { get; set; }

        public void Deconstruct(out ushort operandSizeBits, out bool isSigned, out string constantOperand, out decimal dpd)
        {
            operandSizeBits = OperandSizeBits;
            isSigned = IsSigned;
            constantOperand = ConstantOperand;
            dpd = Dpd;
        }
    }

    private sealed class TimingReport : ITimingReport
    {
        private readonly Dictionary<string, decimal> _timings = new();

        public void SetLatencyNs(
            dynamic operatorType,
            int operandSizeBits,
            bool isSigned,
            string constantOperand,
            decimal dpd)
        {
            _timings[GetKey(operatorType, operandSizeBits, isSigned, constantOperand)] = dpd;

            // If the operand size is 1 that means that the operation also works with single-bit non-composite types
            // where the latter may not have an explicit size. E.g. and std_logic_vector1 would be the same as std_logic
            // but the latter is not a sized data type (and thus it's apparent size will be 0, despite it being stored
            // on at least one bit). Therefore, saving a 0 bit version here too.
            if (operandSizeBits == 1)
            {
                SetLatencyNs(operatorType, 0, isSigned, constantOperand, dpd);
            }
        }

        public decimal GetLatencyNs(BinaryOperatorType binaryOperator, int operandSizeBits, bool isSigned, string constantOperand = null) =>
            GetLatencyNsInternal(binaryOperator, operandSizeBits, isSigned, constantOperand);

        public decimal GetLatencyNs(UnaryOperatorType unaryOperator, int operandSizeBits, bool isSigned) =>
            GetLatencyNsInternal(unaryOperator, operandSizeBits, isSigned, string.Empty);

        private decimal GetLatencyNsInternal(dynamic operatorType, int operandSizeBits, bool isSigned, string constantOperand) =>
            _timings.TryGetValue(GetKey(operatorType, operandSizeBits, isSigned, constantOperand), out decimal latency) ? latency : -1;

        private static string GetKey(dynamic operatorType, int operandSizeBits, bool isSigned, string constantOperand) =>
                operatorType.ToString() +
                operandSizeBits.ToTechnicalString() +
                isSigned.ToString() +
                (string.IsNullOrEmpty(constantOperand) ? "-" : constantOperand);
    }
}
