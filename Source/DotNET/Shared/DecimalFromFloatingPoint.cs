// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// Port of the floating point to decimal conversion of .NET 10 (<c>DecCalc.VarDecFromR4</c> and <c>VarDecFromR8</c>),
/// so the result is identical on every runtime.
/// </summary>
/// <remarks>
/// .NET 11 converts floating point values to a decimal exactly, while earlier runtimes round the value to
/// 7 (float) or 15 (double) significant digits. The algorithm below is the .NET 10 one: scale the magnitude
/// to an integer of the significant digit count, round it half to even, and apply the power of ten as the
/// decimal scale, or multiply it in when the scale would be negative. The only structural difference is that the
/// scale reduction removes trailing zeros in a loop where .NET 10 removes them in steps of 8, 4, 2 and 1;
/// both remove exactly the same number of zeros.
/// </remarks>
internal static class DecimalFromFloatingPoint
{
    const int MaxScale = 28;
    const int MinExponent = -94;
    const int MaxExponent = 96;
    const int Log10Of2Scaled = 19728;

    static readonly UInt128 _maxMantissa = (UInt128.One << 96) - 1;

    static ReadOnlySpan<double> DoublePowers10 =>
    [
        1, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9,
        1e10, 1e11, 1e12, 1e13, 1e14, 1e15, 1e16, 1e17, 1e18, 1e19,
        1e20, 1e21, 1e22, 1e23, 1e24, 1e25, 1e26, 1e27, 1e28
    ];

    /// <summary>
    /// Converts a floating point value to a <see cref="decimal"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="exponent">The binary exponent of the value, as .NET 10 derives it from the biased exponent bits.</param>
    /// <param name="digits">The significant digits of the source type, 7 for <see cref="float"/> and 15 for <see cref="double"/>.</param>
    /// <returns>The converted <see cref="decimal"/>.</returns>
    /// <exception cref="OverflowException">Thrown when the value is NaN, an infinity or outside the range of <see cref="decimal"/>.</exception>
    internal static decimal Create(double value, int exponent, int digits)
    {
        if (exponent < MinExponent)
        {
            return 0m;
        }

        if (exponent > MaxExponent)
        {
            throw new OverflowException("Value was either too large or too small for a Decimal.");
        }

        var scaled = ScaleToDigits(Math.Abs(value), exponent, digits, out var power);
        var mantissa = (ulong)Math.Round(scaled, MidpointRounding.ToEven);
        if (mantissa == 0)
        {
            return 0m;
        }

        return power < 0 ? Expand(mantissa, -power, value < 0) : Reduce(mantissa, power, digits, value < 0);
    }

    static double ScaleToDigits(double magnitude, int exponent, int digits, out int power)
    {
        var upperLimit = DoublePowers10[digits];
        power = digits - 1 - ((exponent * Log10Of2Scaled) >> 16);
        var scaled = magnitude;
        if (power >= 0)
        {
            power = Math.Min(power, MaxScale);
            scaled *= DoublePowers10[power];
        }
        else if (power != -1 || scaled >= upperLimit)
        {
            scaled /= DoublePowers10[-power];
        }
        else
        {
            power = 0;
        }

        if (scaled < upperLimit / 10 && power < MaxScale)
        {
            scaled *= 10;
            power++;
        }

        return scaled;
    }

    static decimal Expand(ulong mantissa, int power, bool isNegative)
    {
        UInt128 product = mantissa;
        for (var i = 0; i < power; i++)
        {
            product *= 10;
        }

        if (product > _maxMantissa)
        {
            throw new OverflowException("Value was either too large or too small for a Decimal.");
        }

        return new decimal((int)(uint)product, (int)(uint)(product >> 32), (int)(uint)(product >> 64), isNegative, 0);
    }

    static decimal Reduce(ulong mantissa, int power, int digits, bool isNegative)
    {
        var removable = Math.Min(power, digits - 1);
        while (removable > 0 && mantissa % 10 == 0)
        {
            mantissa /= 10;
            power--;
            removable--;
        }

        return new decimal((int)(uint)mantissa, (int)(uint)(mantissa >> 32), 0, isNegative, (byte)power);
    }
}
