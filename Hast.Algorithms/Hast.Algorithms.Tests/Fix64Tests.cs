using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Hast.Algorithms.Tests
{
    public class Fix64Tests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        private readonly long[] _testCases = new[]
        {
            // Small numbers
            0L, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
            -1, -2, -3, -4, -5, -6, -7, -8, -9, -10,

            // Integer numbers
            0x_0001_0000_0000, -0x_0001_0000_0000, 0x_0002_0000_0000, -0x_0002_0000_0000, 0x_0003_0000_0000, -0x_0003_0000_0000,
            0x_0004_0000_0000, -0x_0004_0000_0000, 0x_0005_0000_0000, -0x_0005_0000_0000, 0x_0006_0000_0000, -0x_0006_0000_0000,

            // Fractions (1/2, 1/4, 1/8)
            0x_8000_0000, -0x_8000_0000, 0x_4000_0000, -0x_4000_0000, 0x_2000_0000, -0x_2000_0000,

            // Problematic carry
            0x_FFFF_FFFF, -0x_FFFF_FFFF, 0x_0001_FFFF_FFFF, -0x_0001_FFFF_FFFF, 0x_0003_FFFF_FFFF, -0x_0003_FFFF_FFFF,

            // Smallest and largest values
            long.MaxValue, long.MinValue,

            // Large random numbers
            6_791_302_811_978_701_836, -8_192_141_831_180_282_065, 6_222_617_001_063_736_300, -7_871_200_276_881_732_034,
            8_249_382_838_880_205_112, -7_679_310_892_959_748_444, 7_708_113_189_940_799_513, -5_281_862_979_887_936_768,
            8_220_231_180_772_321_456, -5_204_203_381_295_869_580, 6_860_614_387_764_479_339, -9_080_626_825_133_349_457,
            6_658_610_233_456_189_347, -6_558_014_273_345_705_245, 6_700_571_222_183_426_493,

            // Small random numbers
            -436_730_658, -2_259_913_246, 329_347_474, 2_565_801_981, 3_398_143_698, 137_497_017, 1_060_347_500,
            -3_457_686_027, 1_923_669_753, 2_891_618_613, 2_418_874_813, 2_899_594_950, 2_265_950_765, -1_962_365_447,
            3_077_934_393

            // Tiny random numbers
            - 171,
            -359, 491, 844, 158, -413, -422, -737, -575, -330,
            -376, 435, -311, 116, 715, -1_024, -487, 59, 724, 993,
        };

        public Fix64Tests(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

        [Fact]
        public void Precision() => Assert.Equal(0.00000000023283064365386962890625m, Fix64.Precision);

        [Fact]
        public void LongToFix64AndBack()
        {
            var sources = new[] { long.MinValue, int.MinValue - 1L, int.MinValue, -1L, 0L, 1L, int.MaxValue, int.MaxValue + 1L, long.MaxValue };
            var expecteds = new[] { 0L, int.MaxValue, int.MinValue, -1L, 0L, 1L, int.MaxValue, int.MinValue, -1L };
            for (int i = 0; i < sources.Length; ++i)
            {
                var expected = expecteds[i];
                var f = (Fix64)sources[i];
                var actual = (long)f;
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        [SuppressMessage("Blocker Code Smell", "S2699:Tests should include assertions", Justification = nameof(EqualWithinPrecision) + " is an assertion.")]
        public void DoubleToFix64AndBack()
        {
            var sources = new[]
            {
                int.MinValue,
                -Math.PI,
                -Math.E,
                -1.0,
                -0.0,
                0.0,
                1.0,
                Math.PI,
                Math.E,
                int.MaxValue,
            };

            foreach (var value in sources)
            {
                EqualWithinPrecision(value, (double)(Fix64)value);
            }
        }

        [Fact]
        public void DecimalToFix64AndBack()
        {
            Assert.Equal(Fix64.MaxValue(), (Fix64)(decimal)Fix64.MaxValue());
            Assert.Equal(Fix64.MinValue(), (Fix64)(decimal)Fix64.MinValue());

            var sources = new[]
            {
                int.MinValue,
                -(decimal)Math.PI,
                -(decimal)Math.E,
                -1.0m,
                -0.0m,
                0.0m,
                1.0m,
                (decimal)Math.PI,
                (decimal)Math.E,
                int.MaxValue,
            };

            foreach (var value in sources)
            {
                EqualWithinPrecision(value, (decimal)(Fix64)value);
            }
        }

        [Fact]
        public void Addition()
        {
            var terms1 = new[] { Fix64.MinValue(), (Fix64)(-1), Fix64.Zero(), Fix64.One(), Fix64.MaxValue() };
            var terms2 = new[] { (Fix64)(-1), (Fix64)2, (Fix64)(-1.5m), (Fix64)(-2), Fix64.One() };
            var expecteds = new[] { Fix64.MinValue(), Fix64.One(), (Fix64)(-1.5m), (Fix64)(-1), Fix64.MaxValue() };
            for (int i = 0; i < terms1.Length; ++i)
            {
                var actual = terms1[i] + terms2[i];
                var expected = expecteds[i];
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void Substraction()
        {
            var terms1 = new[] { Fix64.MinValue(), (Fix64)(-1), Fix64.Zero(), Fix64.One(), Fix64.MaxValue() };
            var terms2 = new[] { Fix64.One(), (Fix64)(-2), (Fix64)1.5m, (Fix64)2, (Fix64)(-1) };
            var expecteds = new[] { Fix64.MinValue(), Fix64.One(), (Fix64)(-1.5m), (Fix64)(-1), Fix64.MaxValue() };
            for (int i = 0; i < terms1.Length; ++i)
            {
                var actual = terms1[i] - terms2[i];
                var expected = expecteds[i];
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void BasicMultiplication()
        {
            var leftTerms = new[] { 0m, 1m, -1m, 5m, -5m, 0.5m, -0.5m, -1.0m };
            var rightTerms = new[] { 16m, 16m, 16m, 16m, 16m, 16m, 16m, -1.0m };
            var expecteds = new[] { 0L, 16, -16, 80, -80, 8, -8, 1 };
            for (int i = 0; i < leftTerms.Length; ++i)
            {
                var expected = expecteds[i];
                var actual = (long)((Fix64)leftTerms[i] * (Fix64)rightTerms[i]);
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void MultiplicationTestCases()
        {
            var sw = new Stopwatch();
            int failures = 0;
            foreach (var (testCaseX, testCaseY) in GetTestCasePairs())
            {
                var x = Fix64.FromRaw(testCaseX);
                var y = Fix64.FromRaw(testCaseY);
                var xM = (decimal)x;
                var yM = (decimal)y;
                var expected = Constrain(xM * yM, (decimal)Fix64.MinValue(), (decimal)Fix64.MaxValue());
                sw.Start();
                var actual = x * y;
                sw.Stop();
                var actualM = (decimal)actual;
                var maxDelta = (decimal)Fix64.FromRaw(1);
                if (Math.Abs(actualM - expected) > maxDelta)
                {
                    _testOutputHelper.WriteLine(
                        "Failed for FromRaw({0}) * FromRaw({1}): expected {2} but got {3}",
                        testCaseX,
                        testCaseY,
                        (Fix64)expected,
                        actualM);
                    ++failures;
                }
            }

            _testOutputHelper.WriteLine("{0} total, {1} per multiplication", sw.ElapsedMilliseconds, (double)sw.Elapsed.Milliseconds / (_testCases.Length * _testCases.Length));
            Assert.True(failures < 1);
        }

        [Fact]
        public void DivisionTestCases()
        {
            var sw = new Stopwatch();
            int failures = 0;
            foreach (var (testCaseX, testCaseY) in GetTestCasePairs())
            {
                var x = Fix64.FromRaw(testCaseX);
                var y = Fix64.FromRaw(testCaseY);
                var xM = (decimal)x;
                var yM = (decimal)y;

                if (testCaseY == 0)
                {
                    // Hastlayer doesn't handle exceptions.
                    Assert.True(x / y == default);
                }
                else
                {
                    var expected = Constrain(xM / yM, (decimal)Fix64.MinValue(), (decimal)Fix64.MaxValue());
                    sw.Start();
                    var actual = x / y;
                    sw.Stop();
                    var actualM = (decimal)actual;
                    var maxDelta = (decimal)Fix64.FromRaw(1);
                    if (Math.Abs(actualM - expected) > maxDelta)
                    {
                        _testOutputHelper.WriteLine(
                            "Failed for FromRaw({0}) / FromRaw({1}): expected {2} but got {3}",
                            testCaseX,
                            testCaseY,
                            (Fix64)expected,
                            actualM);
                        ++failures;
                    }
                }
            }

            _testOutputHelper.WriteLine("{0} total, {1} per division", sw.ElapsedMilliseconds, (double)sw.Elapsed.Milliseconds / (_testCases.Length * _testCases.Length));
            Assert.True(failures < 1);
        }

        [Fact]
        public void Sign()
        {
            var sources = new[] { Fix64.MinValue(), (Fix64)(-1), Fix64.Zero(), Fix64.One(), Fix64.MaxValue() };
            var expecteds = new[] { -1, -1, 0, 1, 1 };
            for (int i = 0; i < sources.Length; ++i)
            {
                var actual = Fix64.Sign(sources[i]);
                var expected = expecteds[i];
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void Abs()
        {
            Assert.Equal(Fix64.MaxValue(), Fix64.Abs(Fix64.MinValue()));
            var sources = new[] { -1, 0, 1, int.MaxValue };
            var expecteds = new[] { 1, 0, 1, int.MaxValue };
            for (int i = 0; i < sources.Length; ++i)
            {
                var actual = Fix64.Abs((Fix64)sources[i]);
                var expected = (Fix64)expecteds[i];
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void FastAbs()
        {
            Assert.Equal(Fix64.MinValue(), Fix64.FastAbs(Fix64.MinValue()));
            var sources = new[] { -1, 0, 1, int.MaxValue };
            var expecteds = new[] { 1, 0, 1, int.MaxValue };
            for (int i = 0; i < sources.Length; ++i)
            {
                var actual = Fix64.FastAbs((Fix64)sources[i]);
                var expected = (Fix64)expecteds[i];
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void Floor()
        {
            var sources = new[] { -5.1m, -1, 0, 1, 5.1m };
            var expecteds = new[] { -6m, -1, 0, 1, 5m };
            for (int i = 0; i < sources.Length; ++i)
            {
                var actual = (decimal)Fix64.Floor((Fix64)sources[i]);
                var expected = expecteds[i];
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void Ceiling()
        {
            var sources = new[] { -5.1m, -1, 0, 1, 5.1m };
            var expecteds = new[] { -5m, -1, 0, 1, 6m };
            for (int i = 0; i < sources.Length; ++i)
            {
                var actual = (decimal)Fix64.Ceiling((Fix64)sources[i]);
                var expected = expecteds[i];
                Assert.Equal(expected, actual);
            }

            Assert.Equal(Fix64.MaxValue(), Fix64.Ceiling(Fix64.MaxValue()));
        }

        [Fact]
        public void Round()
        {
            var sources = new[] { -5.5m, -5.1m, -4.5m, -4.4m, -1, 0, 1, 4.5m, 4.6m, 5.4m, 5.5m };
            var expecteds = new[] { -6m, -5m, -4m, -4m, -1, 0, 1, 4m, 5m, 5m, 6m };
            for (int i = 0; i < sources.Length; ++i)
            {
                var actual = (decimal)Fix64.Round((Fix64)sources[i]);
                var expected = expecteds[i];
                Assert.Equal(expected, actual);
            }

            Assert.Equal(Fix64.MaxValue(), Fix64.Round(Fix64.MaxValue()));
        }

        [Fact]
        public void Sqrt()
        {
            foreach (var testCase in _testCases)
            {
                var f = Fix64.FromRaw(testCase);
                if (Fix64.Sign(f) < 0)
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => Fix64.Sqrt(f));
                }
                else
                {
                    var expected = Math.Sqrt((double)f);
                    var actual = (double)Fix64.Sqrt(f);
                    var delta = (decimal)Math.Abs(expected - actual);
                    Assert.True(delta <= Fix64.Precision);
                }
            }
        }

        [Fact]
        public void Modulus()
        {
            var deltas = new List<decimal>();
            foreach (var (operand1, operand2) in GetTestCasePairs())
            {
                var f1 = Fix64.FromRaw(operand1);
                var f2 = Fix64.FromRaw(operand2);

                if (operand2 == 0)
                {
                    // Hastlayer doesn't handle exceptions.
                    Assert.True(f1 / f2 == default);
                }
                else
                {
                    var d1 = (decimal)f1;
                    var d2 = (decimal)f2;
                    var actual = (decimal)(f1 % f2);
                    var expected = d1 % d2;
                    var delta = Math.Abs(expected - actual);
                    deltas.Add(delta);
                    Assert.True(delta <= 60 * Fix64.Precision, $"{f1} % {f2} = expected {expected} but got {actual}");
                }
            }

            _testOutputHelper.WriteLine("Max error: {0} ({1} times precision)", deltas.Max(), deltas.Max() / Fix64.Precision);
            _testOutputHelper.WriteLine("Average precision: {0} ({1} times precision)", deltas.Average(), deltas.Average() / Fix64.Precision);
            _testOutputHelper.WriteLine("failed: {0}%", deltas.Count(d => d > Fix64.Precision) * 100.0 / deltas.Count);
        }

        [Fact]
        public void Negation()
        {
            foreach (var operand1 in _testCases)
            {
                var f = Fix64.FromRaw(operand1);
                if (f == Fix64.MinValue())
                {
                    Assert.Equal(-f, Fix64.MaxValue());
                }
                else
                {
                    var expected = -(decimal)f;
                    var actual = (decimal)-f;
                    Assert.Equal(expected, actual);
                }
            }
        }

        [Fact]
        public void EqualsMethod()
        {
            foreach (var (op1, op2) in GetTestCasePairs())
            {
                var d1 = (decimal)op1;
                var d2 = (decimal)op2;
                Assert.True(op1.Equals(op2) == d1.Equals(d2));
            }
        }

        [Fact]
        [SuppressMessage(
            "Major Bug",
            "S1244:Floating point numbers should not be tested for equality",
            Justification = "They should match precisely.")]
        [SuppressMessage(
            "Major Bug",
            "S2589:Change this condition so that it does not always evaluate to 'false'.",
            Justification = "We are trying to verify that, so it's a success state rather than a certainty.")]
        public void EqualityAndInequalityOperators()
        {
            var sources = _testCases.Select(Fix64.FromRaw).ToList();
            foreach (var op1 in sources)
            {
                foreach (var op2 in sources)
                {
                    var d1 = (double)op1;
                    var d2 = (double)op2;
                    Assert.True(op1 == op2 == (d1 == d2));
                    Assert.True(op1 != op2 == (d1 != d2));
                    Assert.False(op1 == op2 && op1 != op2);
                }
            }
        }

        [Fact(Skip = "On ignore because temporarily removed the interface implementations from Fix64")]
        public void CompareTo()
        {
            var nums = _testCases.Select(Fix64.FromRaw).ToArray();
            var numsDecimal = nums.Select(t => (decimal)t).ToArray();
            Array.Sort(nums);
            Array.Sort(numsDecimal);
            Assert.True(nums.Select(t => (decimal)t).SequenceEqual(numsDecimal));
        }

        [Fact]
        public void SerializationToAndFromIntegers()
        {
            foreach (var testCase in _testCases.Select(Fix64.FromRaw))
            {
                Assert.Equal(testCase, Fix64.FromRawInts(testCase.ToIntegers()));
            }
        }

        private IEnumerable<(long TestCaseX, long TestCaseY)> GetTestCasePairs() => GetCartesianProduct(_testCases);

        private static IEnumerable<(T Left, T Right)> GetCartesianProduct<T>(ICollection<T> collection) =>
            from left in collection
            from right in collection
            select (left, right);

        private static void EqualWithinPrecision(decimal value1, decimal value2) =>
            Assert.True(Math.Abs(value2 - value1) < Fix64.Precision);

        private static void EqualWithinPrecision(double value1, double value2) =>
            Assert.True(Math.Abs(value2 - value1) < (double)Fix64.Precision);

        [SuppressMessage(
            "Major Code Smell",
            "S3358:Ternary operators should not be nested",
            Justification = "That's why it's encapsulated into a method.")]
        private static decimal Constrain(decimal expected, decimal min, decimal max) =>
            expected > max
                ? max
                : expected < min
                    ? min
                    : expected;
    }
}
