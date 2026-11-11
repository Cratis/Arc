// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Arc.for_DoubleToDecimal;

/// <summary>
/// The vectors of Screenplay's <c>LegacyNumberLowering</c> specs, so both ports of the .NET 10 conversion agree on value and scale.
/// </summary>
public class when_converting_the_screenplay_legacy_number_vectors
{
    [Theory]
    [InlineData("1234567890123456", "1234567890123460")]
    [InlineData("9999999999999996", "10000000000000000")]
    [InlineData("-1234567890123456", "-1234567890123460")]
    [InlineData("1.23456789012E-17", "0.0000000000000000123456789012")]
    [InlineData("0.1", "0.1")]
    [InlineData("9007199254740991", "9007199254740990")]
    [InlineData("1E-28", "0.0000000000000000000000000001")]
    [InlineData("1E-100", "0")]
    [InlineData("1E28", "10000000000000000000000000000")]
    [InlineData("0.9999999999999996", "1.0")]
    public void should_preserve_the_dotnet_10_value_and_scale(string input, string expected) =>
        decimal.GetBits(DoubleToDecimal.Convert(double.Parse(input, CultureInfo.InvariantCulture)))
            .ShouldEqual(decimal.GetBits(decimal.Parse(expected, CultureInfo.InvariantCulture)));

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(7.922816251426434E28)]
    [InlineData(-7.922816251426434E28)]
    public void should_preserve_overflow(double input) =>
        Catch.Exception(() => DoubleToDecimal.Convert(input)).ShouldBeOfExactType<OverflowException>();
}
