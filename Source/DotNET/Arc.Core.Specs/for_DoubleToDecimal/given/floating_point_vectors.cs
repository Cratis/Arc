// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Arc.for_DoubleToDecimal.given;

/// <summary>
/// A deterministic set of floating point values that covers the decimal conversion edges: signed zero,
/// subnormals, powers of ten and two, the overflow boundary and a seeded pseudo-random sample.
/// </summary>
public static class floating_point_vectors
{
    const int RandomCount = 4000;
    const ulong Seed = 0x9E3779B97F4A7C15;

    public static IReadOnlyList<double> Doubles { get; } = BuildDoubles();

    public static IReadOnlyList<float> Singles { get; } = BuildSingles();

    /// <summary>
    /// Runs every conversion and fingerprints the outcomes, so runtimes can be compared without holding each result.
    /// </summary>
    /// <param name="conversions">The conversions to run.</param>
    /// <returns>The hexadecimal SHA-256 of the bits of every result, or an overflow marker.</returns>
    public static string Fingerprint(IEnumerable<Func<decimal>> conversions)
    {
        var builder = new StringBuilder();
        foreach (var conversion in conversions)
        {
            builder.Append(Describe(conversion)).Append(';');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    /// <summary>
    /// Runs a conversion and describes its outcome.
    /// </summary>
    /// <param name="conversion">The conversion to run.</param>
    /// <returns>The raw decimal bits (sign and scale included), or an overflow marker.</returns>
    public static string Describe(Func<decimal> conversion)
    {
        try
        {
            return string.Join(',', decimal.GetBits(conversion()).Select(_ => _.ToString(CultureInfo.InvariantCulture)));
        }
        catch (OverflowException)
        {
            return "overflow";
        }
    }

    static List<double> BuildDoubles()
    {
        var values = new List<double>
        {
            0d, -0d, double.Epsilon, -double.Epsilon, double.NaN, double.PositiveInfinity, double.NegativeInfinity,
            double.MaxValue, double.MinValue, 1.23d, 0.1d, 0.1000000000000005d, 5.000000000000001e-29d,
            9007199254740991d, 7.9e28d
        };

        for (var exponent = -310; exponent <= 310; exponent++)
        {
            values.Add(double.Parse("1e" + exponent, CultureInfo.InvariantCulture));
        }

        for (var exponent = -1080; exponent <= 1030; exponent++)
        {
            var power = Math.ScaleB(1d, exponent);
            values.AddRange([power, -power, double.BitIncrement(power), double.BitDecrement(power)]);
        }

        var state = Seed;
        for (var i = 0; i < RandomCount; i++)
        {
            values.Add(BitConverter.UInt64BitsToDouble(NextRandom(ref state)));
            values.Add(double.Parse($"{(NextRandom(ref state) % 1_000_000_000_000_000UL) + 1}e{((int)(NextRandom(ref state) % 70)) - 35}", CultureInfo.InvariantCulture));
        }

        return values;
    }

    static List<float> BuildSingles()
    {
        var values = new List<float>
        {
            0f, -0f, float.Epsilon, -float.Epsilon, float.NaN, float.PositiveInfinity, float.NegativeInfinity,
            float.MaxValue, float.MinValue, 1.23f, 0.1f, 16777216f, 7.9e28f
        };

        for (var exponent = -50; exponent <= 40; exponent++)
        {
            values.Add(float.Parse("1e" + exponent, CultureInfo.InvariantCulture));
        }

        for (var exponent = -160; exponent <= 130; exponent++)
        {
            var power = MathF.ScaleB(1f, exponent);
            values.AddRange([power, -power, float.BitIncrement(power), float.BitDecrement(power)]);
        }

        var state = Seed;
        for (var i = 0; i < RandomCount; i++)
        {
            values.Add(BitConverter.UInt32BitsToSingle((uint)NextRandom(ref state)));
            values.Add(float.Parse($"{(NextRandom(ref state) % 10_000_000UL) + 1}e{((int)(NextRandom(ref state) % 70)) - 35}", CultureInfo.InvariantCulture));
        }

        return values;
    }

    static ulong NextRandom(ref ulong state)
    {
        state ^= state << 13;
        state ^= state >> 7;
        state ^= state << 17;
        return state;
    }
}
