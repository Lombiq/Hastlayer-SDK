using Hast.TestInputs.Dynamic;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Hast.DynamicTests.Tests;

public class BinaryAndUnaryOperatorExpressionCasesTests
{
    // MinValue would cause a division by zero when the input is cast to smaller data types that's why MiValue + 1
    // is tested everywhere.
    // Testing at least an odd and even number too.
    // Since there are no generic constraints for numeric types unfortunately the non-int ones need to be copied
    // (see: https://stackoverflow.com/questions/32664/is-there-a-constraint-that-restricts-my-generic-method-to-numeric-types).

    // ByteBinaryOperatorExpressionVariations(int.MinValue + 1) fails on ulong multiplication with:
    // "index: 148, hardware result: { 255, 255, 7, 0 }, software result: { 255, 255, 255, 255 }"
    // Same with UshortBinaryOperatorExpressionVariations.
    // Most possibly overflow is not handled the same as in .NET.
    [Fact]
    public Task ByteBinaryOperatorExpressionVariations() =>
    ExecuteIntTestAsync(
         b => b.ByteBinaryOperatorExpressionVariations(null),
         b => b.ByteBinaryOperatorExpressionVariations,
         noMinValue: true);

    [Fact]
    public Task SbyteBinaryOperatorExpressionVariations() =>
        ExecuteIntTestAsync(
             b => b.SbyteBinaryOperatorExpressionVariations(null),
             b => b.SbyteBinaryOperatorExpressionVariations);

    [Fact]
    public Task ShortBinaryOperatorExpressionVariations() =>
        ExecuteIntTestAsync(
             b => b.ShortBinaryOperatorExpressionVariations(null),
             b => b.ShortBinaryOperatorExpressionVariations);

    [Fact]
    public Task UshortBinaryOperatorExpressionVariations() =>
        ExecuteIntTestAsync(
             b => b.UshortBinaryOperatorExpressionVariations(null),
             b => b.UshortBinaryOperatorExpressionVariations,
             noMinValue: true);

    [Fact]
    public Task IntBinaryOperatorExpressionVariations() =>
        ExecuteIntTestAsync(
             b => b.IntBinaryOperatorExpressionVariations(null),
             b => b.IntBinaryOperatorExpressionVariations);

    [Fact]
    public Task UintBinaryOperatorExpressionVariations() =>
        ExecuteTestAsync(
            b => b.UintBinaryOperatorExpressionVariations(null),
            b =>
            {
                b.UintBinaryOperatorExpressionVariations(uint.MinValue + 1);
                b.UintBinaryOperatorExpressionVariations(123);
                b.UintBinaryOperatorExpressionVariations(124);
                b.UintBinaryOperatorExpressionVariations(uint.MaxValue);
            });

    [Fact]
    public Task LongBinaryOperatorExpressionVariationsLow() =>
        ExecuteLongTestAsync(
             b => b.LongBinaryOperatorExpressionVariationsLow(null),
             b => b.LongBinaryOperatorExpressionVariationsLow);

    [Fact]
    public Task LongBinaryOperatorExpressionVariationsHigh() =>
        ExecuteLongTestAsync(
             b => b.LongBinaryOperatorExpressionVariationsHigh(null),
             b => b.LongBinaryOperatorExpressionVariationsHigh);

    [Fact]
    public Task UlongBinaryOperatorExpressionVariationsLow() =>
        ExecuteTestAsync(
            b => b.UlongBinaryOperatorExpressionVariationsLow(null),
            b =>
            {
                b.UlongBinaryOperatorExpressionVariationsLow(ulong.MinValue + 1);
                b.UlongBinaryOperatorExpressionVariationsLow(123);
                b.UlongBinaryOperatorExpressionVariationsLow(124);
                b.UlongBinaryOperatorExpressionVariationsLow(long.MaxValue);
            });

    [Fact]
    public Task UlongBinaryOperatorExpressionVariationsHigh() =>
        ExecuteTestAsync(
            b => b.UlongBinaryOperatorExpressionVariationsHigh(null),
            b =>
            {
                b.UlongBinaryOperatorExpressionVariationsHigh(ulong.MinValue + 1);
                b.UlongBinaryOperatorExpressionVariationsHigh(123);
                b.UlongBinaryOperatorExpressionVariationsHigh(124);
                b.UlongBinaryOperatorExpressionVariationsHigh(ulong.MaxValue);
            });

    [Fact]
    public Task AllUnaryOperatorExpressionVariations() =>
        ExecuteLongTestAsync(
             b => b.AllUnaryOperatorExpressionVariations(null),
             b => b.AllUnaryOperatorExpressionVariations);

    private static Task ExecuteIntTestAsync(
        Expression<Action<BinaryAndUnaryOperatorExpressionCases>> caseSelector,
        Func<BinaryAndUnaryOperatorExpressionCases, IntTestCaseMethod> caseMethod,
        bool noMinValue = false) =>
        ExecuteTestAsync(
            caseSelector,
            b =>
            {
                if (!noMinValue) caseMethod(b)(int.MinValue + 1);
                caseMethod(b)(123);
                caseMethod(b)(124);
                caseMethod(b)(int.MaxValue);
            });

    private delegate void IntTestCaseMethod(int input);

    private static Task ExecuteLongTestAsync(
        Expression<Action<BinaryAndUnaryOperatorExpressionCases>> caseSelector,
        Func<BinaryAndUnaryOperatorExpressionCases, LongTestCaseMethod> caseMethod) =>
        ExecuteTestAsync(
            caseSelector,
            b =>
            {
                caseMethod(b)(long.MinValue + 1);
                caseMethod(b)(123);
                caseMethod(b)(124);
                caseMethod(b)(long.MaxValue);
            });

    private delegate void LongTestCaseMethod(long input);

    private static Task ExecuteTestAsync(
        Expression<Action<BinaryAndUnaryOperatorExpressionCases>> caseSelector,
        Action<BinaryAndUnaryOperatorExpressionCases> testExecutor) =>
        TestExecutor.ExecuteSelectedTestAsync(caseSelector, testExecutor);
}
