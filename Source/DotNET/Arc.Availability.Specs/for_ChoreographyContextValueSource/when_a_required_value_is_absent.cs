// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Contexts;

namespace Cratis.Arc.Availability.for_ChoreographyContextValueSource;

public class when_a_required_value_is_absent : given.a_value_host
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(async () => await ValuesFor(null).GetAsync("project"));

    [Fact] void should_refuse_with_the_absent_reason() => _error.ShouldBeOfExactType<RequiredContextValueAbsentException>();
    [Fact] void should_name_the_value() => ((RequiredContextValueAbsentException)_error).Name.ShouldEqual("project");
    [Fact] void should_not_be_a_retryable_failure() => (_error is ContextValueUnavailableException).ShouldBeFalse();
}
