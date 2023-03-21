using Hast.Layer;
using Hast.Synthesis.Models;
using ICSharpCode.Decompiler.CSharp.Syntax;
using System;

namespace Hast.Synthesis.Helpers;

public static class DeviceDriverHelper
{
    public static decimal ComputeClockCyclesForBinaryOperation(
        IDeviceManifest deviceManifest, ITimingReport timingReport, BinaryOperatorExpression expression, int operandSizeBits, bool isSigned)
    {
        var latencyNs = timingReport.GetLatencyNs(expression.Operator, operandSizeBits, isSigned);

        if (IsRightOperandConstant(expression, out var constantValue))
        {
            var constantLatencyNs = timingReport.GetLatencyNs(expression.Operator, operandSizeBits, isSigned, constantValue);
            if (constantLatencyNs > 0) latencyNs = constantLatencyNs;
        }

        return ComputeClockCyclesFromLatency(deviceManifest, ReturnLatencyOrThrowIfInvalid(latencyNs, expression));
    }

    public static decimal ComputeClockCyclesForUnaryOperation(
        IDeviceManifest deviceManifest, ITimingReport timingReport, UnaryOperatorExpression expression, int operandSizeBits, bool isSigned) =>
        ComputeClockCyclesFromLatency(
            deviceManifest,
            ReturnLatencyOrThrowIfInvalid(timingReport.GetLatencyNs(expression.Operator, operandSizeBits, isSigned), expression));

    public static bool IsRightOperandConstant(BinaryOperatorExpression expression, out string constantValue)
    {
        constantValue = string.Empty;

        if (expression.Right is PrimitiveExpression primitiveExpression)
        {
            // LiteralValue somehow is an empty string for PrimitiveExpressions.
            var valueObject = primitiveExpression.Value;
            if (valueObject != null)
            {
                constantValue = valueObject.ToString();
                return true;
            }
        }

        return false;
    }

    public static decimal ComputeClockCyclesFromLatency(IDeviceManifest deviceManifest, decimal latencyNs)
    {
        var latencyClockCycles = latencyNs * deviceManifest.ClockFrequencyHz * 0.000000001M;

        // If there is no latency then let's try with a basic default (unless the operation is "instant" there should be
        // latency data).
        if (latencyClockCycles < 0) return 0.1M;

        return latencyClockCycles;
    }

    public static decimal ReturnLatencyOrThrowIfInvalid(decimal latencyNs, Expression expression)
    {
        if (latencyNs >= 0) return latencyNs;

        throw new InvalidOperationException(
            $"No latency data found for the {nameof(expression)} {expression}. This is most possibly a bug in " +
            $"Hastlayer, please submit a bug report with the affected code snippet: " +
            $"https://github.com/Lombiq/Hastlayer-SDK/issues.");
    }
}
