using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Hast.Algorithms.Tests
{
    public class Fix64Tests
    {
        private readonly long[] _testCases = new[]
        {
            // Small numbers
            0L, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
            -1, -2, -3, -4, -5, -6, -7, -8, -9, -10,
  
            // Integer numbers
            0x100000000, -0x100000000, 0x200000000, -0x200000000, 0x300000000, -0x300000000,
            0x400000000, -0x400000000, 0x500000000, -0x500000000, 0x600000000, -0x600000000,
  
            // Fractions (1/2, 1/4, 1/8)
            0x80000000, -0x80000000, 0x40000000, -0x40000000, 0x20000000, -0x20000000,
  
            // Problematic carry
            0xFFFFFFFF, -0xFFFFFFFF, 0x1FFFFFFFF, -0x1FFFFFFFF, 0x3FFFFFFFF, -0x3FFFFFFFF,
  
            // Smallest and largest values
            long.MaxValue, long.MinValue,
  
            // Large random numbers
            6791302811978701836, -8192141831180282065, 6222617001063736300, -7871200276881732034,
            8249382838880205112, -7679310892959748444, 7708113189940799513, -5281862979887936768,
            8220231180772321456, -5204203381295869580, 6860614387764479339, -9080626825133349457,
            6658610233456189347, -6558014273345705245, 6700571222183426493,
  
            // Small random numbers
            -436730658, -2259913246, 329347474, 2565801981, 3398143698, 137497017, 1060347500,
            -3457686027, 1923669753, 2891618613, 2418874813, 2899594950, 2265950765, -1962365447,
            3077934393

            // Tiny random numbers
            - 171,
            -359, 491, 844, 158, -413, -422, -737, -575, -330,
            -376, 435, -311, 116, 715, -1024, -487, 59, 724, 993
        };

        [Fact]
        public void Precision()
        {
            Assert.Equal(0.00000000023283064365386962890625m, Fix64.Precision);
        }

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
            var sources = new[] {
                (double)int.MinValue,
                -(double)Math.PI,
                -(double)Math.E,
                -1.0,
                -0.0,
                0.0,
                1.0,
                (double)Math.PI,
                (double)Math.E,
                (double)int.MaxValue
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

            var sources = new[] {
                int.MinValue,
                -(decimal)Math.PI,
                -(decimal)Math.E,
                -1.0m,
                -0.0m,
                0.0m,
                1.0m,
                (decimal)Math.PI,
                (decimal)Math.E,
                int.MaxValue
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
            var terms2 = new[] { Fix64.One(), (Fix64)(-2), (Fix64)(1.5m), (Fix64)(2), (Fix64)(-1) };
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
            var term1s = new[] { 0m, 1m, -1m, 5m, -5m, 0.5m, -0.5m, -1.0m };
            var term2s = new[] { 16m, 16m, 16m, 16m, 16m, 16m, 16m, -1.0m };
            var expecteds = new[] { 0L, 16, -16, 80, -80, 8, -8, 1 };
            for (int i = 0; i < term1s.Length; ++i)
            {
                var expected = expecteds[i];
                var actual = (long)((Fix64)term1s[i] * (Fix64)term2s[i]);
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void MultiplicationTestCases()
        {
            var sw = new Stopwatch();
            int failures = 0;
            for (int i = 0; i < _testCases.Length; ++i)
            {
                for (int j = 0; j < _testCases.Length; ++j)
                {
                    var x = Fix64.FromRaw(_testCases[i]);
                    var y = Fix64.FromRaw(_testCases[j]);
                    var xM = (decimal)x;
                    var yM = (decimal)y;
                    var expected = xM * yM;
                    expected =
                        expected > (decimal)Fix64.MaxValue()
                            ? (decimal)Fix64.MaxValue()
                            : expected < (decimal)Fix64.MinValue()
                                  ? (decimal)Fix64.MinValue()
                                  : expected;
                    sw.Start();
                    var actual = x * y;
                    sw.Stop();
                    var actualM = (decimal)actual;
                    var maxDelta = (decimal)Fix64.FromRaw(1);
                    if (Math.Abs(actualM - expected) > maxDelta)
                    {
                        Console.WriteLine("Failed for FromRaw({0}) * FromRaw({1}): expected {2} but got {3}",
                                          _testCases[i],
                                          _testCases[j],
                                          (Fix64)expected,
                                          actualM);
                        ++failures;
                    }
                }
            }
            Console.WriteLine("{0} total, {1} per multiplication", sw.ElapsedMilliseconds, (double)sw.Elapsed.Milliseconds / (_testCases.Length * _testCases.Length));
            Assert.True(failures < 1);
        }

        [Fact]
        public void DivisionTestCases()
        {
            var sw = new Stopwatch();
            int failures = 0;
            for (int i = 0; i < _testCases.Length; ++i)
            {
                for (int j = 0; j < _testCases.Length; ++j)
                {
                    var x = Fix64.FromRaw(_testCases[i]);
                    var y = Fix64.FromRaw(_testCases[j]);
                    var xM = (decimal)x;
                    var yM = (decimal)y;

                    if (_testCases[j] == 0)
                    {
                        // Hastlayer doesn't handle exceptions.
                        Assert.True(x / y == default);
                        //Assert.Throws<DivideByZeroException>(() => Ignore(x / y));
                    }
                    else
                    {
                        var expected = xM / yM;
                        expected =
                            expected > (decimal)Fix64.MaxValue()
                                ? (decimal)Fix64.MaxValue()
                                : expected < (decimal)Fix64.MinValue()
                                      ? (decimal)Fix64.MinValue()
                                      : expected;
                        sw.Start();
                        var actual = x / y;
                        sw.Stop();
                        var actualM = (decimal)actual;
                        var maxDelta = (decimal)Fix64.FromRaw(1);
                        if (Math.Abs(actualM - expected) > maxDelta)
                        {
                            Console.WriteLine("Failed for FromRaw({0}) / FromRaw({1}): expected {2} but got {3}",
                                              _testCases[i],
                                              _testCases[j],
                                              (Fix64)expected,
                                              actualM);
                            ++failures;
                        }
                    }
                }
            }
            Console.WriteLine("{0} total, {1} per division", sw.ElapsedMilliseconds, (double)sw.Elapsed.Milliseconds / (_testCases.Length * _testCases.Length));
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
            for (int i = 0; i < _testCases.Length; ++i)
            {
                var f = Fix64.FromRaw(_testCases[i]);
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
            foreach (var operand1 in _testCases)
            {
                foreach (var operand2 in _testCases)
                {
                    var f1 = Fix64.FromRaw(operand1);
                    var f2 = Fix64.FromRaw(operand2);

                    if (operand2 == 0)
                    {
                        // Hastlayer doesn't handle exceptions.
                        Assert.True(f1 / f2 == default);
                        //Assert.Throws<DivideByZeroException>(() => Ignore(f1 / f2));
                    }
                    else
                    {
                        var d1 = (decimal)f1;
                        var d2 = (decimal)f2;
                        var actual = (decimal)(f1 % f2);
                        var expected = d1 % d2;
                        var delta = Math.Abs(expected - actual);
                        deltas.Add(delta);
                        Assert.True(delta <= 60 * Fix64.Precision, string.Format("{0} % {1} = expected {2} but got {3}", f1, f2, expected, actual));
                    }
                }
            }
            Console.WriteLine("Max error: {0} ({1} times precision)", deltas.Max(), deltas.Max() / Fix64.Precision);
            Console.WriteLine("Average precision: {0} ({1} times precision)", deltas.Average(), deltas.Average() / Fix64.Precision);
            Console.WriteLine("failed: {0}%", deltas.Count(d => d > Fix64.Precision) * 100.0 / deltas.Count);
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
                    var expected = -((decimal)f);
                    var actual = (decimal)(-f);
                    Assert.Equal(expected, actual);
                }
            }
        }

        [Fact]
        public void EqualsMethod()
        {
            foreach (var op1 in _testCases)
            {
                foreach (var op2 in _testCases)
                {
                    var d1 = (decimal)op1;
                    var d2 = (decimal)op2;
                    Assert.True(op1.Equals(op2) == d1.Equals(d2));
                }
            }
        }

        [Fact]
        public void EqualityAndInequalityOperators()
        {
            var sources = _testCases.Select(Fix64.FromRaw).ToList();
            foreach (var op1 in sources)
            {
                foreach (var op2 in sources)
                {
                    var d1 = (double)op1;
                    var d2 = (double)op2;
                    Assert.True((op1 == op2) == (d1 == d2));
                    Assert.True((op1 != op2) == (d1 != d2));
                    Assert.False((op1 == op2) && (op1 != op2));
                }
            }
        }

        [Fact(Skip="On ignore because temporarily removed the interface implementations from Fix64")]
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

        static void EqualWithinPrecision(decimal value1, decimal value2)
        {
            Assert.True(Math.Abs(value2 - value1) < Fix64.Precision);
        }

        static void EqualWithinPrecision(double value1, double value2)
        {
            Assert.True(Math.Abs(value2 - value1) < (double)Fix64.Precision);
        }
    }
}
