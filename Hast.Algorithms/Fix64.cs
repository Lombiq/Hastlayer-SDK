using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Hast.Algorithms
{
    /// <summary>
    /// Represents a Q31.32 fixed-point number.
    /// </summary>
    /// <remarks>
    /// Taken from https://github.com/asik/FixedMath.Net and modified to be Hastlayer-compatible. See the original 
    /// license below:
    /// 
    /// Copyright 2012 André Slupik
    /// 
    /// Licensed under the Apache License, Version 2.0 (the "License");
    /// you may not use this file except in compliance with the License.
    /// You may obtain a copy of the License at
    /// 
    ///     http://www.apache.org/licenses/LICENSE-2.0
    /// 
    /// Unless required by applicable law or agreed to in writing, software
    /// distributed under the License is distributed on an "AS IS" BASIS,
    /// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    /// See the License for the specific language governing permissions and
    /// limitations under the License.
    /// 
    /// This project uses code from the libfixmath library, which is under the following license:
    /// 
    /// Copyright (C) 2012 Petteri Aimonen
    /// 
    /// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
    /// 
    /// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
    /// 
    /// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
    /// </remarks>
    // Implemented interfaces removed so Hastlayer transformation is quicker, without having to also process mscorlib.
    public struct Fix64 //: IEquatable<Fix64>, IComparable<Fix64>
    {
        private readonly long _rawValue;

        private const long MaxRawValue = long.MaxValue;
        private const long MinRawValue = long.MinValue;
        private const long OneRawValue = 1L << FractionalPlaces;
        private const int BitCount = 64;
        private const int FractionalPlaces = 32;
        private const long PiTimes2 = 0x6487ED511;
        private const long Pi = 0x3243F6A88;
        private const long PiOver2 = 0x1921FB544;
        private const int LutSize = (int)(PiOver2 >> 15);

        // Original static fields commented out because those are not yet supported by Hastlayer, see: 
        // https://github.com/Lombiq/Hastlayer-SDK/issues/24
        // Precision is left in because due to decimal it won't be possible to transform any way, but can be used on
        // CPU still.

        // Precision of this type is 2^-32, that is 2,3283064365386962890625E-10
        public static readonly decimal Precision = (decimal)(new Fix64(1L));//0.00000000023283064365386962890625m;
        //public static readonly Fix64 MaxValue = new Fix64(MAX_VALUE);
        //public static readonly Fix64 MinValue = new Fix64(MIN_VALUE);
        //public static readonly Fix64 One = new Fix64(ONE);
        //public static readonly Fix64 Zero = new Fix64();
        ///// <summary>
        ///// The value of Pi
        ///// </summary>
        //public static readonly Fix64 Pi = new Fix64(PI);
        //public static readonly Fix64 PiOver2 = new Fix64(PI_OVER_2);
        //public static readonly Fix64 PiTimes2 = new Fix64(PI_TIMES_2);
        //public static readonly Fix64 PiInv = (Fix64)0.3183098861837906715377675267M;
        //public static readonly Fix64 PiOver2Inv = (Fix64)0.6366197723675813430755350535M;

        public static Fix64 MaxValue() => new Fix64(MaxRawValue);
        public static Fix64 MinValue() => new Fix64(MinRawValue);
        public static Fix64 One() => new Fix64(OneRawValue);
        public static Fix64 Zero() => new Fix64();

        //static readonly Fix64 LutInterval = (Fix64)(LUT_SIZE - 1) / PiOver2;

        /// <summary>
        /// The underlying integer representation
        /// </summary>
        public long RawValue { get { return _rawValue; } }

        /// <summary>
        /// This is the constructor from raw value; it can only be used internally.
        /// </summary>
        /// <param name="rawValue"></param>
        private Fix64(long rawValue)
        {
            _rawValue = rawValue;
        }

        public Fix64(int value)
        {
            _rawValue = value * OneRawValue;
        }


        #region Instance methods

        public override bool Equals(object obj) => obj is Fix64 && ((Fix64)obj)._rawValue == _rawValue;

        public override int GetHashCode() => _rawValue.GetHashCode();

        public bool Equals(Fix64 other) => _rawValue == other._rawValue;

        public int CompareTo(Fix64 other) => _rawValue.CompareTo(other._rawValue);

        public override string ToString() => ((decimal)this).ToString();

        public int[] ToIntegers()
        {
            var low = (int)(_rawValue & uint.MaxValue);
            int high = (int)(_rawValue >> 32);
            return new int[] { low, high };
        }

        #endregion

        #region Mathematical functions

        /// <summary>
        /// Returns a number indicating the sign of a Fix64 number.
        /// Returns 1 if the value is positive, 0 if is 0, and -1 if it is negative.
        /// </summary>
        public static int Sign(Fix64 value) => 
            value._rawValue < 0 ? -1 :
                value._rawValue > 0 ? 1 :
                0;

        /// <summary>
        /// Returns the absolute value of a Fix64 number.
        /// Note: Abs(Fix64.MinValue) == Fix64.MaxValue.
        /// </summary>
        public static Fix64 Abs(Fix64 value)
        {
            if (value._rawValue == MinRawValue)
            {
                return MaxValue();
            }

            // Branch-less implementation, see http://www.strchr.com/optimized_abs_function
            var mask = value._rawValue >> 63;
            return new Fix64((value._rawValue + mask) ^ mask);
        }

        /// <summary>
        /// Returns the absolute value of a Fix64 number.
        /// FastAbs(Fix64.MinValue) is undefined.
        /// </summary>
        public static Fix64 FastAbs(Fix64 value)
        {
            // Branch-less implementation, see http://www.strchr.com/optimized_abs_function
            var mask = value._rawValue >> 63;
            return new Fix64((value._rawValue + mask) ^ mask);
        }


        /// <summary>
        /// Returns the largest integer less than or equal to the specified number.
        /// </summary>
        public static Fix64 Floor(Fix64 value)
        {
            // Just zero out the fractional part

            // Creating the value 0xFFFFFFFF00000000. 
            // This literal can't be directly used due to an ILSpy bug, see:
            // https://github.com/icsharpcode/ILSpy/issues/807
            uint zLow = 0x0000;
            uint zHigh = 0x0000;
            uint z = (0 << 32) | (zLow << 16) | zHigh;

            ulong mask = 0xFFFFFFFF;
            mask <<= 32;
            mask |= z;

            return new Fix64((long)((ulong)value._rawValue & mask));
        }

        /// <summary>
        /// Returns the smallest integral value that is greater than or equal to the specified number.
        /// </summary>
        public static Fix64 Ceiling(Fix64 value)
        {
            // Creating the value 0x00000000FFFFFFFF. 
            // This literal can't be directly used due to an ILSpy bug, see:
            // https://github.com/icsharpcode/ILSpy/issues/807
            uint zLow = 0xFFFF;
            uint zHigh = 0xFFFF;
            uint z = (0 << 32) | (zLow << 16) | zHigh;

            long mask = 0x00000000;
            mask <<= 32;
            mask |= z;

            var hasFractionalPart = (value._rawValue & mask) != 0;
            return hasFractionalPart ? Floor(value) + One() : value;
        }

        /// <summary>
        /// Rounds a value to the nearest integral value.
        /// If the value is halfway between an even and an uneven value, returns the even value.
        /// </summary>
        public static Fix64 Round(Fix64 value)
        {
            // Creating the value 0x00000000FFFFFFFF. 
            // This literal can't be directly used due to an ILSpy bug, see:
            // https://github.com/icsharpcode/ILSpy/issues/807
            uint zLow = 0xFFFF;
            uint zHigh = 0xFFFF;
            uint z = (0 << 32) | (zLow << 16) | zHigh;

            long mask = 0x00000000;
            mask <<= 32;
            mask |= z;

            var fractionalPart = value._rawValue & mask;
            var integralPart = Floor(value);

            // Creating the value 0x80000000.
            uint fractionalPartMask = 0x8000;
            fractionalPartMask <<= 16;
            fractionalPartMask |= 0x0000;

            if (fractionalPart < fractionalPartMask)
            {
                return integralPart;
            }
            if (fractionalPart > fractionalPartMask)
            {
                return integralPart + One();
            }
            // if number is halfway between two values, round to the nearest even number
            // this is the method used by System.Math.Round().
            return (integralPart._rawValue & OneRawValue) == 0
                       ? integralPart
                       : integralPart + One();
        }

        /// <summary>
        /// Returns the square root of a specified number.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The argument was negative.
        /// </exception>
        public static Fix64 Sqrt(Fix64 x)
        {
            var xl = x._rawValue;
            if (xl < 0)
            {
                // We cannot represent infinities like Single and Double, and Sqrt is
                // mathematically undefined for x < 0. So we just throw an exception.
                throw new ArgumentOutOfRangeException("Negative value passed to Sqrt", "x");
            }

            var num = (ulong)xl;
            var result = 0UL;

            // second-to-top bit
            var bit = 1UL << (BitCount - 2);

            while (bit > num)
            {
                bit >>= 2;
            }

            // Creating the value 0x0000000080000000. 
            // This literal can't be directly used due to an ILSpy bug, see:
            // https://github.com/icsharpcode/ILSpy/issues/807
            uint zLow = 0x0000;
            uint zHigh = 0x8000;
            uint z = (0 << 32) | (zLow << 16) | zHigh;

            long mask = 0x00000000;
            mask <<= 32;
            mask |= z;

            // The main part is executed twice, in order to avoid
            // using 128 bit values in computations.
            for (var i = 0; i < 2; ++i)
            {
                // First we get the top 48 bits of the answer.
                while (bit != 0)
                {
                    if (num >= result + bit)
                    {
                        num -= result + bit;
                        result = (result >> 1) + bit;
                    }
                    else
                    {
                        result = result >> 1;
                    }
                    bit >>= 2;
                }

                if (i == 0)
                {
                    // Then process it again to get the lowest 16 bits.
                    if (num > (1UL << (BitCount / 2)) - 1)
                    {
                        // The remainder 'num' is too large to be shifted left
                        // by 32, so we have to add 1 to result manually and
                        // adjust 'num' accordingly.
                        // num = a - (result + 0.5)^2
                        //       = num + result^2 - (result + 0.5)^2
                        //       = num - result - 0.5
                        num -= result;
                        num = (num << (BitCount / 2)) - 0x80000000UL;
                        result = (result << (BitCount / 2)) + 0x80000000UL;
                    }
                    else
                    {
                        num <<= (BitCount / 2);
                        result <<= (BitCount / 2);
                    }

                    bit = 1UL << (BitCount / 2 - 2);
                }
            }
            // Finally, if next bit would have been 1, round the result upwards.
            if (num > result)
            {
                ++result;
            }
            return new Fix64((long)result);
        }

        #endregion

        #region Operators

        /// <summary>
        /// Adds x and y. Performs saturating addition, i.e. in case of overflow, 
        /// rounds to MinValue or MaxValue depending on sign of operands.
        /// </summary>
        public static Fix64 operator +(Fix64 x, Fix64 y)
        {
            var xl = x._rawValue;
            var yl = y._rawValue;
            var sum = xl + yl;

            // If signs of operands are equal and signs of sum and x are different
            if (((~(xl ^ yl) & (xl ^ sum)) & MinRawValue) != 0)
            {
                sum = xl > 0 ? MaxRawValue : MinRawValue;
            }

            return new Fix64(sum);
        }

        /// <summary>
        /// Adds x and y without performing overflow checking. Should be inlined by the CLR.
        /// </summary>
        public static Fix64 FastAdd(Fix64 x, Fix64 y) => new Fix64(x._rawValue + y._rawValue);

        /// <summary>
        /// Subtracts y from x. Performs saturating subtraction, i.e. in case of overflow, 
        /// rounds to MinValue or MaxRawValue depending on sign of operands.
        /// </summary>
        public static Fix64 operator -(Fix64 x, Fix64 y)
        {
            var xl = x._rawValue;
            var yl = y._rawValue;
            var diff = xl - yl;

            // Ff signs of operands are different and signs of sum and x are different
            if ((((xl ^ yl) & (xl ^ diff)) & MinRawValue) != 0)
            {
                diff = xl < 0 ? MinRawValue : MaxRawValue;
            }

            return new Fix64(diff);
        }

        /// <summary>
        /// Subtracts y from x without performing overflow checking. Should be inlined by the CLR.
        /// </summary>
        public static Fix64 FastSub(Fix64 x, Fix64 y) => new Fix64(x._rawValue - y._rawValue);

        public static Fix64 operator *(Fix64 x, Fix64 y)
        {
            var xl = x._rawValue;
            var yl = y._rawValue;

            // Creating the value 0x00000000FFFFFFFF. 
            // This literal can't be directly used due to an ILSpy bug, see:
            // https://github.com/icsharpcode/ILSpy/issues/807
            uint zLow = 0xFFFF;
            uint zHigh = 0xFFFF;
            uint z = (0 << 32) | (zLow << 16) | zHigh;

            long mask = 0x00000000;
            mask <<= 32;
            mask |= z;

            var xlo = (ulong)(xl & mask);
            var xhi = xl >> FractionalPlaces;
            var ylo = (ulong)(yl & mask);
            var yhi = yl >> FractionalPlaces;

            var lolo = xlo * ylo;
            var lohi = (long)xlo * yhi;
            var hilo = xhi * (long)ylo;
            var hihi = xhi * yhi;

            var loResult = lolo >> FractionalPlaces;
            var midResult1 = lohi;
            var midResult2 = hilo;
            var hiResult = hihi << FractionalPlaces;

            bool overflow = false;
            var sum = AddOverflowHelper((long)loResult, midResult1, ref overflow);
            sum = AddOverflowHelper(sum, midResult2, ref overflow);
            sum = AddOverflowHelper(sum, hiResult, ref overflow);

            bool opSignsEqual = ((xl ^ yl) & MinRawValue) == 0;

            // if signs of operands are equal and sign of result is negative,
            // then multiplication overflowed positively
            // the reverse is also true
            if (opSignsEqual)
            {
                if (sum < 0 || (overflow && xl > 0))
                {
                    return MaxValue();
                }
            }
            else
            {
                if (sum > 0)
                {
                    return MinValue();
                }
            }

            // if the top 32 bits of hihi (unused in the result) are neither all 0s or 1s,
            // then this means the result overflowed.
            var topCarry = hihi >> FractionalPlaces;
            if (topCarry != 0 && topCarry != -1 /*&& xl != -17 && yl != -17*/)
            {
                return opSignsEqual ? MaxValue() : MinValue();
            }

            // If signs differ, both operands' magnitudes are greater than 1,
            // and the result is greater than the negative operand, then there was negative overflow.
            if (!opSignsEqual)
            {
                long posOp, negOp;

                if (xl > yl)
                {
                    posOp = xl;
                    negOp = yl;
                }
                else
                {
                    posOp = yl;
                    negOp = xl;
                }

                if (sum > negOp && negOp < -OneRawValue && posOp > OneRawValue)
                {
                    return MinValue();
                }
            }

            return new Fix64(sum);
        }

        /// <summary>
        /// Performs multiplication without checking for overflow.
        /// Useful for performance-critical code where the values are guaranteed not to cause overflow
        /// </summary>
        public static Fix64 FastMul(Fix64 x, Fix64 y)
        {
            var xl = x._rawValue;
            var yl = y._rawValue;

            // Creating the value 0x00000000FFFFFFFF. 
            // This literal can't be directly used due to an ILSpy bug, see:
            // https://github.com/icsharpcode/ILSpy/issues/807
            uint zLow = 0xFFFF;
            uint zHigh = 0xFFFF;
            uint z = (0 << 32) | (zLow << 16) | zHigh;

            long mask = 0x00000000;
            mask <<= 32;
            mask |= z;
            var xlo = (ulong)(xl & mask);
            var xhi = xl >> FractionalPlaces;
            var ylo = (ulong)(yl & mask);
            var yhi = yl >> FractionalPlaces;

            var lolo = xlo * ylo;
            var lohi = (long)xlo * yhi;
            var hilo = xhi * (long)ylo;
            var hihi = xhi * yhi;

            var loResult = lolo >> FractionalPlaces;
            var midResult1 = lohi;
            var midResult2 = hilo;
            var hiResult = hihi << FractionalPlaces;

            var sum = (long)loResult + midResult1 + midResult2 + hiResult;
            return new Fix64(sum);
        }

        public static Fix64 operator /(Fix64 x, Fix64 y)
        {
            var xl = x._rawValue;
            var yl = y._rawValue;

            if (yl == 0)
            {
                throw new DivideByZeroException();
            }

            var remainder = (ulong)(xl >= 0 ? xl : -xl);
            var divider = (ulong)(yl >= 0 ? yl : -yl);
            var quotient = 0UL;
            var bitPos = BitCount / 2 + 1;


            // If the divider is divisible by 2^n, take advantage of it.
            while ((divider & 0xF) == 0 && bitPos >= 4)
            {
                divider >>= 4;
                bitPos -= 4;
            }

            // Creating the value 0xFFFFFFFFFFFFFFFF. 
            // This literal can't be directly used due to an ILSpy bug, see:
            // https://github.com/icsharpcode/ILSpy/issues/807
            uint zLow = 0xFFFF;
            uint zHigh = 0xFFFF;
            uint z = (0 << 32) | (zLow << 16) | zHigh;

            ulong mask = 0xFFFFFFFF;
            mask <<= 32;
            mask |= z;

            while (remainder != 0 && bitPos >= 0)
            {
                int shift = CountLeadingZeroes(remainder);
                if (shift > bitPos)
                {
                    shift = bitPos;
                }
                remainder <<= shift;
                bitPos -= shift;

                var div = remainder / divider;
                remainder = remainder % divider;
                quotient += div << bitPos;

                // Detect overflow
                if ((div & ~(mask >> bitPos)) != 0)
                {
                    return ((xl ^ yl) & MinRawValue) == 0 ? MaxValue() : MinValue();
                }

                remainder <<= 1;
                --bitPos;
            }

            // rounding
            ++quotient;
            var result = (long)(quotient >> 1);
            if (((xl ^ yl) & MinRawValue) != 0)
            {
                result = -result;
            }

            return new Fix64(result);
        }

        public static Fix64 operator %(Fix64 x, Fix64 y) => 
            new Fix64(
                x._rawValue == MinRawValue & y._rawValue == -1 ?
                0 :
                x._rawValue % y._rawValue);

        /// <summary>
        /// Performs modulo as fast as possible; throws if x == MinValue and y == -1.
        /// Use the operator (%) for a more reliable but slower modulo.
        /// </summary>
        public static Fix64 FastMod(Fix64 x, Fix64 y) => new Fix64(x._rawValue % y._rawValue);

        public static Fix64 operator -(Fix64 x) => x._rawValue == MinRawValue ? MaxValue() : new Fix64(-x._rawValue);

        public static bool operator ==(Fix64 x, Fix64 y) => x._rawValue == y._rawValue;

        public static bool operator !=(Fix64 x, Fix64 y) => x._rawValue != y._rawValue;

        public static bool operator >(Fix64 x, Fix64 y) => x._rawValue > y._rawValue;

        public static bool operator <(Fix64 x, Fix64 y) => x._rawValue < y._rawValue;

        public static bool operator >=(Fix64 x, Fix64 y) => x._rawValue >= y._rawValue;

        public static bool operator <=(Fix64 x, Fix64 y) => x._rawValue <= y._rawValue;

        #endregion

        #region Casts

        public static explicit operator Fix64(long value) => new Fix64(value * OneRawValue);

        public static explicit operator long(Fix64 value) => value._rawValue >> FractionalPlaces;

        public static explicit operator Fix64(float value) => new Fix64((long)(value * OneRawValue));

        public static explicit operator float(Fix64 value) => (float)value._rawValue / OneRawValue;

        public static explicit operator Fix64(double value) => new Fix64((long)(value * OneRawValue));

        public static explicit operator double(Fix64 value) => (double)value._rawValue / OneRawValue;

        public static explicit operator Fix64(decimal value) => new Fix64((long)(value * OneRawValue));

        public static explicit operator decimal(Fix64 value) => (decimal)value._rawValue / OneRawValue;

        #endregion

        #region Factories

        public static Fix64 FromRaw(long rawValue) => new Fix64(rawValue);

        public static Fix64 FromRawInts(int[] integers)
        {
            long rawValue = integers[1];
            rawValue = rawValue << 32;
            rawValue = rawValue | (uint)integers[0];

            return new Fix64(rawValue);
        }

        #endregion


        private static long AddOverflowHelper(long x, long y, ref bool overflow)
        {
            var sum = x + y;
            // x + y overflows if sign(x) ^ sign(y) != sign(sum)
            overflow |= ((x ^ y ^ sum) & MinRawValue) != 0;
            return sum;
        }

        private static int[] ToIntegers(ulong number)
        {
            var low = (int)(number & uint.MaxValue);
            int high = (int)(number >> 32);
            return new int[] { low, high };
        }

        private static int[] ToIntegers(long number)
        {
            var low = (int)(number & uint.MaxValue);
            int high = (int)(number >> 32);
            return new int[] { low, high };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CountLeadingZeroes(ulong x)
        {
            int result = 0;

            // Creating the value 0xF000000000000000. 
            // This literal can't be directly used due to an ILSpy bug, see:
            // https://github.com/icsharpcode/ILSpy/issues/807
            uint zLow = 0x0000;
            uint zHigh = 0x0000;
            uint z = (0 << 32) | (zLow << 16) | zHigh;

            ulong mask1 = 0xF0000000;
            mask1 <<= 32;
            mask1 |= z;

            while ((x & mask1) == 0) { result += 4; x <<= 4; }

            // Creating the value 0x8000000000000000. 
            // This literal can't be directly used due to an ILSpy bug, see:
            // https://github.com/icsharpcode/ILSpy/issues/807
            ulong mask2 = 0x80000000;
            mask2 <<= 32;
            mask2 |= z;

            while ((x & mask2) == 0) { result += 1; x <<= 1; }


            return result;
        }
    }
}
