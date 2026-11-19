// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Arc.for_DoubleToDecimal;

public class when_converting_a_float_that_is_decimal_text
{
    [Theory]
    [InlineData(1.23f, "1.23")]
    [InlineData(0.1f, "0.1")]
    [InlineData(-1.23f, "-1.23")]
    [InlineData(16777216f, "16777220")] // 8 digits round to 7 significant digits, as before .NET 11
    [InlineData(1e-7f, "0.0000001")]
    [InlineData(1e20f, "100000000000000000000")]
    public void should_produce_the_dotnet_10_value(float value, string expected) =>
        SingleToDecimal.Convert(value).ShouldEqual(decimal.Parse(expected, CultureInfo.InvariantCulture));
}
