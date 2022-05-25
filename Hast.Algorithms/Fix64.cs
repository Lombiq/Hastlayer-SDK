using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Hast.Algorithms;

/// <summary>
/// Represents a Q31.32 fixed-point number.
/// </summary>
/// <remarks>
/// <para>
/// Taken from https://github.com/asik/FixedMath.Net and modified to be Hastlayer-compatible. See the original license
/// below:
///
/// Copyright 2012 André Slupik
///
/// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
/// the License. You may obtain a copy of the License at
///
/// http://www.apache.org/licenses/LICENSE-2.0
///
/// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
/// an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
/// specific language governing permissions and limitations under the License.
///
/// This project uses code from the libfixmath library, which is under the following license:
///
/// Copyright (C) 2012 Petteri Aimonen
///
/// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
/// documentation files (the "Software"), to deal in the Software without restriction, including without limitation the
/// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
/// permit persons to whom the Software is furnished to do so, subject to the following conditions:
///
/// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the
/// Software.
///
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
/// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS
/// OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
/// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
/// </para>
/// </remarks>
public struct Fix64 : IEquatable<Fix64>, IComparable<Fix64>
{
    private const long MaxRawValue = long.MaxValue;
    private const long MinRawValue = long.MinValue;
    private const long OneRawValue = 1L << FractionalPlaces;
    private const int BitCount = 64;
    private const int FractionalPlaces = 32;
    private const uint UnsignedHalf = 0x_8000_0000;

    // Original static fields commented out because those are not yet supported by Hastlayer, see:
    // https://github.com/Lombiq/Hastlayer-SDK/issues/24 Precision is left in because due to decimal it won't be
    // possible to transform any way, but can be used on CPU still.

    // Precision of this type is 2^-32, that is 2,3283064365386962890625E-10
    public static readonly decimal Precision = (decimal)new Fix64(1L);

    public static Fix64 MaxValue() => new(MaxRawValue);

    public static Fix64 MinValue() => new(MinRawValue);

    public static Fix64 One() => new(OneRawValue);

    public static Fix64 Zero() => default;

    /// <summary>
    /// Gets the underlying integer representation.
    /// </summary>
    public long RawValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Fix64"/> struct. It's from raw value; only use it internally.
    /// </summary>
    private Fix64(long rawValue) => RawValue = rawValue;

    public Fix64(int value) => RawValue = value * OneRawValue;

    #region Instance methods

    public override int GetHashCode() => RawValue.GetHashCode();

    public override bool Equals(object obj) => obj is Fix64 fix64 && fix64.RawValue == RawValue;

    public bool Equals(Fix64 other) => RawValue == other.RawValue;

    public int CompareTo(Fix64 other) => RawValue.CompareTo(other.RawValue);

    public override string ToString() => ((decimal)this).ToString(CultureInfo.InvariantCulture);

    public int[] ToIntegers()
    {
        var low = (int)(RawValue & uint.MaxValue);
        int high = (int)(RawValue >> 32);
        return new[] { low, high };
    }

    #endregion Instance methods

    #region Mathematical functions

    /// <summary>
    /// Returns a number indicating the sign of a Fix64 number. Returns 1 if the value is positive, 0 if is 0, and -1 if
    /// it is negative.
    /// </summary>
    public static int Sign(Fix64 value) =>
        value.RawValue switch
        {
            { } when value.RawValue < 0 => -1,
            { } when value.RawValue > 0 => 1,
            _ => 0,
        };

    /// <summary>
    /// Returns the absolute value of a Fix64 number. Note: Abs(Fix64.MinValue) == Fix64.MaxValue.
    /// </summary>
    public static Fix64 Abs(Fix64 value)
    {
        if (value.RawValue == MinRawValue)
        {
            return MaxValue();
        }

        // Branch-less implementation, see http://www.strchr.com/optimized_abs_function
        var mask = value.RawValue >> 63;
        return new Fix64((value.RawValue + mask) ^ mask);
    }

    /// <summary>
    /// Returns the absolute value of a Fix64 number. FastAbs(Fix64.MinValue) is undefined.
    /// </summary>
    public static Fix64 FastAbs(Fix64 value)
    {
        // Branch-less implementation, see http://www.strchr.com/optimized_abs_function
        var mask = value.RawValue >> 63;
        return new Fix64((value.RawValue + mask) ^ mask);
    }

    /// <summary>
    /// Returns the largest integer less than or equal to the specified number.
    /// </summary>
    public static Fix64 Floor(Fix64 value) =>
        // Just zero out the fractional part
        new((long)((ulong)value.RawValue & 0x_FFFF_FFFF_0000_0000));

    /// <summary>
    /// Returns the smallest integral value that is greater than or equal to the specified number.
    /// </summary>
    public static Fix64 Ceiling(Fix64 value)
    {
        var hasFractionalPart = (value.RawValue & 0x_0000_0000_FFFF_FFFF) != 0;
        return hasFractionalPart ? Floor(value) + One() : value;
    }

    /// <summary>
    /// Rounds a value to the nearest integral value. If the value is halfway between an even and an uneven value,
    /// returns the even value.
    /// </summary>
    public static Fix64 Round(Fix64 value)
    {
        var fractionalPart = value.RawValue & 0x_0000_0000_FFFF_FFFF;
        var integralPart = Floor(value);

        if (fractionalPart < UnsignedHalf)
        {
            return integralPart;
        }

        if (fractionalPart > UnsignedHalf)
        {
            return integralPart + One();
        }

        // if number is halfway between two values, round to the nearest even number this is the method used by
        // System.Math.Round().
        return (integralPart.RawValue & OneRawValue) == 0
                   ? integralPart
                   : integralPart + One();
    }

    /// <summary>
    /// Returns the square root of a specified number.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The argument was negative.</exception>
    public static Fix64 Sqrt(Fix64 x)
    {
        var xl = x.RawValue;
        if (xl < 0)
        {
            // We cannot represent infinities like Single and Double, and Sqrt is mathematically undefined for x <
            // 0. So we just throw an exception.
            throw new ArgumentOutOfRangeException(nameof(x), "Negative value passed to Sqrt");
        }

        var num = (ulong)xl;
        var result = 0UL;

        // second-to-top bit
        var bit = 1UL << (BitCount - 2);
        while (bit > num) bit >>= 2;

        // The main part is executed twice, in order to avoid using 128 bit values in computations.
        SqrtInnerHigh(ref num, ref result, ref bit);
        SqrtInnerLow(ref num, ref result, out bit);
        SqrtInnerHigh(ref num, ref result, ref bit);

        // Finally, if next bit would have been 1, round the result upwards.
        if (num > result) ++result;

        return new Fix64((long)result);
    }

    private static void SqrtInnerHigh(ref ulong num, ref ulong result, ref ulong bit)
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
                result >>= 1;
            }

            bit >>= 2;
        }
    }

    private static void SqrtInnerLow(ref ulong num, ref ulong result, out ulong bit)
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
            num = (num << (BitCount / 2)) - UnsignedHalf;
            result = (result << (BitCount / 2)) + UnsignedHalf;
        }
        else
        {
            num <<= BitCount / 2;
            result <<= BitCount / 2;
        }

        bit = 1UL << ((BitCount / 2) - 2);
    }

    #endregion Mathematical functions

    #region Operators

    /// <summary>
    /// Adds x and y. Performs saturating addition, i.e. in case of overflow, rounds to MinValue or MaxValue depending
    /// on sign of operands.
    /// </summary>
    public static Fix64 operator +(Fix64 x, Fix64 y)
    {
        var xl = x.RawValue;
        var yl = y.RawValue;
        var sum = xl + yl;

        // If signs of operands are equal and signs of sum and x are different
        if ((~(xl ^ yl) & (xl ^ sum) & MinRawValue) != 0)
        {
            sum = xl > 0 ? MaxRawValue : MinRawValue;
        }

        return new Fix64(sum);
    }

    /// <summary>
    /// Adds x and y without performing overflow checking. Should be inlined by the CLR.
    /// </summary>
    public static Fix64 FastAdd(Fix64 x, Fix64 y) => new(x.RawValue + y.RawValue);

    /// <summary>
    /// Subtracts y from x. Performs saturating subtraction, i.e. in case of overflow, rounds to MinValue or MaxRawValue
    /// depending on sign of operands.
    /// </summary>
    public static Fix64 operator -(Fix64 x, Fix64 y)
    {
        var xl = x.RawValue;
        var yl = y.RawValue;
        var diff = xl - yl;

        // Ff signs of operands are different and signs of sum and x are different
        if (((xl ^ yl) & (xl ^ diff) & MinRawValue) != 0)
        {
            diff = xl < 0 ? MinRawValue : MaxRawValue;
        }

        return new Fix64(diff);
    }

    /// <summary>
    /// Subtracts y from x without performing overflow checking. Should be inlined by the CLR.
    /// </summary>
    public static Fix64 FastSub(Fix64 x, Fix64 y) => new(x.RawValue - y.RawValue);

    public static Fix64 operator *(Fix64 x, Fix64 y)
    {
        var xl = x.RawValue;
        var yl = y.RawValue;

        var xlo = (ulong)(xl & 0x_0000_0000_FFFF_FFFF);
        var xhi = xl >> FractionalPlaces;
        var ylo = (ulong)(yl & 0x_0000_0000_FFFF_FFFF);
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

        // If signs of operands are equal and sign of result is negative, then multiplication overflowed positively the
        // reverse is also true.
        if (opSignsEqual)
        {
            if (sum < 0 || (overflow && xl > 0))
            {
                return MaxValue();
            }
        }
        else if (sum > 0)
        {
            return MinValue();
        }

        // if the top 32 bits of hihi (unused in the result) are neither all 0s or 1s, then this means the result
        // overflowed.
        var topCarry = hihi >> FractionalPlaces;
        if (topCarry is not 0 and not -1)
        {
            return opSignsEqual ? MaxValue() : MinValue();
        }

        if (opSignsEqual) return new Fix64(sum);

        // If signs differ, both operands' magnitudes are greater than 1, and the result is greater than the negative
        // operand, then there was negative overflow.
        var (posOp, negOp) = xl > yl ? (xl, yl) : (yl, xl);

        return sum > negOp && negOp < -OneRawValue && posOp > OneRawValue
            ? MinValue()
            : new Fix64(sum);
    }

    /// <summary>
    /// Performs multiplication without checking for overflow. Useful for performance-critical code where the values are
    /// guaranteed not to cause overflow.
    /// </summary>
    public static Fix64 FastMul(Fix64 x, Fix64 y)
    {
        var xl = x.RawValue;
        var yl = y.RawValue;

        var xlo = (ulong)(xl & 0x_0000_0000_FFFF_FFFF);
        var xhi = xl >> FractionalPlaces;
        var ylo = (ulong)(yl & 0x_0000_0000_FFFF_FFFF);
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
        var xl = x.RawValue;
        var yl = y.RawValue;

        if (yl == 0)
        {
            return default; // Hastlayer can't process exceptions at the moment.
        }

        // Needs the temporary *Signed variables to work around this ILSpy bug:
        // https://github.com/icsharpcode/ILSpy/issues/807
        var remainderSigned = xl >= 0 ? xl : -xl;
        var remainder = (ulong)remainderSigned;
        var dividerSigned = yl >= 0 ? yl : -yl;
        var divider = (ulong)dividerSigned;
        var quotient = 0UL;
        var bitPos = (BitCount / 2) + 1;

        // If the divider is divisible by 2^n, take advantage of it.
        while ((divider & 0xF) == 0 && bitPos >= 4)
        {
            divider >>= 4;
            bitPos -= 4;
        }

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
            remainder %= divider;
            quotient += div << bitPos;

            // Detect overflow
            if ((div & ~(0x_FFFF_FFFF_FFFF_FFFF >> bitPos)) != 0)
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
        new(
            x.RawValue == MinRawValue && y.RawValue == -1 ?
            0 :
            x.RawValue % y.RawValue);

    /// <summary>
    /// Performs modulo as fast as possible; throws if x == MinValue and y == -1. Use the operator (%) for a more
    /// reliable but slower modulo.
    /// </summary>
    public static Fix64 FastMod(Fix64 x, Fix64 y) => new(x.RawValue % y.RawValue);

    public static Fix64 operator -(Fix64 x) => x.RawValue == MinRawValue ? MaxValue() : new Fix64(-x.RawValue);

    public static bool operator ==(Fix64 x, Fix64 y) => x.RawValue == y.RawValue;

    public static bool operator !=(Fix64 x, Fix64 y) => x.RawValue != y.RawValue;

    public static bool operator >(Fix64 x, Fix64 y) => x.RawValue > y.RawValue;

    public static bool operator <(Fix64 x, Fix64 y) => x.RawValue < y.RawValue;

    public static bool operator >=(Fix64 x, Fix64 y) => x.RawValue >= y.RawValue;

    public static bool operator <=(Fix64 x, Fix64 y) => x.RawValue <= y.RawValue;

    #endregion Operators

    #region Casts

    public static explicit operator Fix64(long value) => new(value * OneRawValue);

    public static explicit operator long(Fix64 value) => value.RawValue >> FractionalPlaces;

    public static explicit operator Fix64(float value) => new((long)(value * OneRawValue));

    public static explicit operator float(Fix64 value) => (float)value.RawValue / OneRawValue;

    public static explicit operator Fix64(double value) => new((long)(value * OneRawValue));

    public static explicit operator double(Fix64 value) => (double)value.RawValue / OneRawValue;

    public static explicit operator Fix64(decimal value) => new((long)(value * OneRawValue));

    public static explicit operator decimal(Fix64 value) => (decimal)value.RawValue / OneRawValue;

    #endregion Casts

    #region Factories

    public static Fix64 FromRaw(long rawValue) => new(rawValue);

    public static Fix64 FromRawInts(int[] integers)
    {
        long rawValue = integers[1];
        rawValue <<= 32;
        rawValue |= (uint)integers[0];

        return new Fix64(rawValue);
    }

    #endregion Factories

    private static long AddOverflowHelper(long x, long y, ref bool overflow)
    {
        var sum = x + y;
        // x + y overflows if sign(x) ^ sign(y) != sign(sum)
        overflow |= ((x ^ y ^ sum) & MinRawValue) != 0;
        return sum;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CountLeadingZeroes(ulong x)
    {
        int result = 0;

        while ((x & 0x_F000_0000_0000_0000) == 0)
        {
            result += 4;
            x <<= 4;
        }

        while ((x & 0x_8000_0000_0000_0000) == 0)
        {
            result += 1;
            x <<= 1;
        }

        return result;
    }
}
