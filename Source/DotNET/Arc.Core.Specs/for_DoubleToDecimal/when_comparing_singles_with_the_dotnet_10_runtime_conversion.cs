// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET11_0_OR_GREATER
using Cratis.Arc.for_DoubleToDecimal.given;

namespace Cratis.Arc.for_DoubleToDecimal;

public class when_comparing_singles_with_the_dotnet_10_runtime_conversion
{
    [Fact]
    public void should_match_the_runtime_for_every_vector() =>
        floating_point_vectors.Singles
            .Select(value => (Value: value, Actual: floating_point_vectors.Describe(() => SingleToDecimal.Convert(value)), Expected: floating_point_vectors.Describe(() => Convert.ToDecimal(value))))
            .Where(_ => _.Actual != _.Expected)
            .Select(_ => $"{_.Value:R}: {_.Actual} expected {_.Expected}")
            .ShouldBeEmpty();
}
#endif
