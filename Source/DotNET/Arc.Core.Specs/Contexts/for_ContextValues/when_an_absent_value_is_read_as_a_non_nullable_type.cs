// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Contexts.for_ContextValues;

public class when_an_absent_value_is_read_as_a_non_nullable_type : given.a_provided_vessel
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => ContextValues.For(_services, () => null).ValuesAs<NeedsDepth>());

    [Fact] void should_fail_with_a_type_mismatch() => _error.ShouldBeOfExactType<ContextValuesTypeMismatch>();

    public record NeedsDepth(int Depth);
}
