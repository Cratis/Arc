// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.for_DoubleToDecimal;

public class when_converting_a_signed_zero_double
{
    [Theory]
    [InlineData(0d)]
    [InlineData(double.Epsilon)]
    [InlineData(-double.Epsilon)]
    public void should_produce_an_unsigned_zero(double value) =>
        string.Join(',', decimal.GetBits(DoubleToDecimal.Convert(value))).ShouldEqual("0,0,0,0");

    [Fact]
    void should_produce_an_unsigned_zero_for_negative_zero() =>
        string.Join(',', decimal.GetBits(DoubleToDecimal.Convert(BitConverter.Int64BitsToDouble(long.MinValue)))).ShouldEqual("0,0,0,0");
}
