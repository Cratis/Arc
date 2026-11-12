// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.for_DoubleToDecimal;

public class when_converting_a_float_that_cannot_be_a_decimal
{
    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(8e28f)]
    [InlineData(-8e28f)]
    [InlineData(float.MaxValue)]
    [InlineData(float.MinValue)]
    public void should_overflow(float value) =>
        Catch.Exception(() => SingleToDecimal.Convert(value)).ShouldBeOfExactType<OverflowException>();
}
