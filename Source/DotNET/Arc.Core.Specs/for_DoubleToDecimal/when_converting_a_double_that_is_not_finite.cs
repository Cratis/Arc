// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.for_DoubleToDecimal;

public class when_converting_a_double_that_is_not_finite
{
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void should_overflow(double value) =>
        Catch.Exception(() => DoubleToDecimal.Convert(value)).ShouldBeOfExactType<OverflowException>();
}
