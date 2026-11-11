// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// Converts a <see cref="float"/> to a <see cref="decimal"/> with the .NET 10 result on every runtime.
/// </summary>
/// <remarks>
/// .NET 11 converts a float to a decimal exactly, while earlier runtimes round to 7 significant digits.
/// </remarks>
internal static class SingleToDecimal
{
    const int SingleExponentBias = 126;
    const int SignificantDigits = 7;

    /// <summary>
    /// Converts a <see cref="float"/> to a <see cref="decimal"/> rounded to 7 significant digits.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted <see cref="decimal"/>.</returns>
    /// <exception cref="OverflowException">Thrown when the value is NaN, an infinity or outside the range of <see cref="decimal"/>.</exception>
    internal static decimal Convert(float value)
    {
        var exponent = (byte)(BitConverter.SingleToUInt32Bits(value) >> 23) - SingleExponentBias;
        return DecimalFromFloatingPoint.Create(value, exponent, SignificantDigits);
    }
}
