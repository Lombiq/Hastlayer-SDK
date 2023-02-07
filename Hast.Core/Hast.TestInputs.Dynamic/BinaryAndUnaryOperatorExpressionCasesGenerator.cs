using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Hast.TestInputs.Dynamic;

public static class BinaryAndUnaryOperatorExpressionCasesGenerator
{
    private const string UInt = "uint";
    private const string Long = "long";
    private const string ULong = "ulong";
    private static readonly string[] _needsShiftCastTypes = new[] { UInt, Long, ULong };

    public static void Generate()
    {
        var types = new[] { "byte", "sbyte", "short", "ushort", "int" }.Union(_needsShiftCastTypes).ToArray();
        var codeBuilder = new StringBuilder();

        int memoryIndex;

        for (int i = 0; i < types.Length; i++)
        {
            memoryIndex = 0;
            var leftType = types[i];
            var leftIsLong = leftType is Long or ULong;

            AddMethodStart(leftIsLong ? "Low" : string.Empty, codeBuilder, leftType);

            for (int j = 0; j < types.Length; j++)
            {
                if (leftIsLong && j == types.Length / 2)
                {
                    codeBuilder.AppendLine("}");
                    AddMethodStart("High", codeBuilder, leftType);
                }

                GenerateTypes(leftType, types[j], codeBuilder, memoryIndex);
            }

            codeBuilder.AppendLine("}");
        }

        File.WriteAllText("AllBinaryOperatorExpressionVariations.cs", codeBuilder.ToString());

        memoryIndex = 0;
        codeBuilder.Clear();

        for (int i = 0; i < types.Length; i++)
        {
            var type = types[i];
            var isUlong = type == ULong;
            var variableName = type + "Operand";

            codeBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $@"
                    var {variableName} = {(type != Long ? $"({type})" : string.Empty)}input;");

            codeBuilder.AppendLine(AddSaveResult(memoryIndex, "~" + variableName, isUlong));
            codeBuilder.AppendLine(AddSaveResult(memoryIndex, "+" + variableName, isUlong));

            if (type != ULong)
            {
                codeBuilder.AppendLine(AddSaveResult(memoryIndex, "-" + variableName, isUlong));
            }
        }

        File.WriteAllText("AllUnaryOperatorExpressionVariations.cs", codeBuilder.ToString());
    }

    private static void GenerateTypes(
        string leftType,
        string rightType,
        StringBuilder codeBuilder,
        int memoryIndex)
    {
        var left = leftType + "Left";
        var right = rightType + "Right";

        var rightValue =
            rightType != "int" ||
            leftType.Contains(Long, StringComparison.InvariantCulture) ||
            leftType == UInt
                ? $"({rightType})"
                : string.Empty;

        codeBuilder.AppendLine(
            CultureInfo.InvariantCulture,
            $@"
                var {right} = {rightValue}input;");

        var shiftRightOperandCast = _needsShiftCastTypes.Contains(rightType) ? "(int)" : string.Empty;

        codeBuilder.AppendLine(AddSaveResult(memoryIndex, $"{left} << {shiftRightOperandCast}{right}"));
        codeBuilder.AppendLine(AddSaveResult(memoryIndex, $"{left} >> {shiftRightOperandCast}{right}"));

        // These variations can't be applied to the operands directly except for shift (which has a cast).
        if (
            (leftType == "sbyte" && rightType == ULong) ||
            (leftType == "short" && rightType == ULong) ||
            (leftType == "int" && rightType == ULong) ||
            (leftType == Long && rightType == ULong) ||
            (leftType == ULong && rightType == "sbyte") ||
            (leftType == ULong && rightType == "short") ||
            (leftType == ULong && rightType == "int") ||
            (leftType == ULong && rightType == Long))
        {
            return;
        }

        codeBuilder.AppendLine(AddiSaveResults(
            memoryIndex,
            $"{left} + {right}",
            $"{left} - {right}",
            $"{left} * {right}",
            $"{left} / {right}",
            $"{left} % {right}",
            $"{left} & {right}",
            $"{left} | {right}",
            $"{left} ^ {right}"));
    }

    [SuppressMessage(
        "Major Code Smell",
        "S107:Methods should not have too many parameters",
        Justification = "It's easy to follow and reducing the argument count would only make the code more complicated and harder to read.")]
    private static string AddiSaveResults(
        int memoryIndex,
        string code0,
        string code1,
        string code2,
        string code3,
        string code4,
        string code5,
        string code6,
        string code7)
    {
        var originalMemoryIndex = memoryIndex;
        memoryIndex += 16;
        // The long cast will be unnecessary most of the time but determining when it's needed is complex so good
        // enough. Awkward indentation is so the generated code can be properly formatted.
        return FormattableString.Invariant($@"SaveResult(
                memory,
                {originalMemoryIndex},
                (long)({code0}), // {originalMemoryIndex}
                (long)({code1}), // {originalMemoryIndex + 2}
                (long)({code2}), // {originalMemoryIndex + 4}
                (long)({code3}), // {originalMemoryIndex + 6}
                (long)({code4}), // {originalMemoryIndex + 8}
                (long)({code5}), // {originalMemoryIndex + 10}
                (long)({code6}), // {originalMemoryIndex + 12}
                (long)({code7})); // {originalMemoryIndex + 14}");
    }

    private static void AddMethodStart(string nameSuffix, StringBuilder codeBuilder, string leftType)
    {
        var namePrefix = leftType.ToUpperInvariant()[0] + leftType[1..];
        var left = leftType + "Left";
        codeBuilder.AppendLine(
            CultureInfo.InvariantCulture,
            $@"
                public virtual void {namePrefix}BinaryOperatorExpressionVariations{nameSuffix}(SimpleMemory memory)
                {{");

        if (leftType is Long or ULong)
        {
            codeBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $@"{leftType} input = (({leftType})memory.ReadInt32(0) << 32) | (uint)memory.ReadInt32(1);
                        var {left} = input;");
        }
        else if (leftType == UInt)
        {
            codeBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $@"var input = memory.ReadUInt32(0);
                        var {left} = input;");
        }
        else
        {
            codeBuilder.AppendLine(
                CultureInfo.InvariantCulture,
                $@"var input = memory.ReadInt32(0);
                        var {left} = {(leftType != "int" ? $"({leftType})" : string.Empty)}input;");
        }
    }

    private static string AddSaveResult(int memoryIndex, string code, bool addLongCast = true)
    {
        var originalMemoryIndex = memoryIndex;
        memoryIndex += 2;
        // The long cast will be unnecessary most of the time but determining when it's needed is complex so good
        // enough.
        if (addLongCast) code = $"(long)({code})";
        return FormattableString.Invariant($@"SaveResult(memory, {originalMemoryIndex}, {code});");
    }
}
