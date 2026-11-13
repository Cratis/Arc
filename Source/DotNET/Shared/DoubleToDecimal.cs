// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// Converts a <see cref="double"/> to a <see cref="decimal"/> with the .NET 10 result on every runtime.
/// </summary>
/// <remarks>
/// .NET 11 converts a double to a decimal exactly (<c>1.23d</c> becomes <c>1.229999999999999982236431606m</c>),
/// while earlier runtimes round to 15 significant digits (<c>1.23m</c>). Values that arrive as a double,
/// such as numbers read from JSON or BSON, are decimal text by origin, so the 15 digit form is the intended value.
/// </remarks>
internal static class DoubleToDecimal
{
    const int DoubleExponentBias = 1022;
    const int SignificantDigits = 15;

    /// <summary>
    /// Converts a <see cref="double"/> to a <see cref="decimal"/> rounded to 15 significant digits.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted <see cref="decimal"/>.</returns>
    /// <exception cref="OverflowException">Thrown when the value is NaN, an infinity or outside the range of <see cref="decimal"/>.</exception>
    internal static decimal Convert(double value)
    {
        var exponent = (int)((BitConverter.DoubleToUInt64Bits(value) >> 52) & 0x7FF) - DoubleExponentBias;
        return DecimalFromFloatingPoint.Create(value, exponent, SignificantDigits);
    }
}
