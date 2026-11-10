// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.for_DoubleToDecimal;

public class when_converting_a_double_beyond_the_decimal_range
{
    [Theory]
    [InlineData(8e28d)]
    [InlineData(-8e28d)]
    [InlineData(79228162514264337593543950336d)] // 2^96
    [InlineData(-79228162514264337593543950336d)]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    public void should_overflow(double value) =>
        Catch.Exception(() => DoubleToDecimal.Convert(value)).ShouldBeOfExactType<OverflowException>();
}
