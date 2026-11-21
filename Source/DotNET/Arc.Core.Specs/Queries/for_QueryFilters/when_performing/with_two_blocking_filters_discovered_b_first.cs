// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryFilters.when_performing;

public class with_two_blocking_filters_discovered_b_first : given.two_blocking_query_filters
{
    void Establish() => PerformWith(new BFilter(), new AFilter());

    async Task Because() => await Perform();

    [Fact] void should_report_the_error_from_the_filter_sorting_first() => _result.ExceptionMessages.ShouldContainOnly("error from A");
}
