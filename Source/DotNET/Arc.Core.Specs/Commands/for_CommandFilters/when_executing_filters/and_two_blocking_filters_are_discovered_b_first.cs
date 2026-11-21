// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandFilters.when_executing_filters;

public class and_two_blocking_filters_are_discovered_b_first : given_two_blocking_filters
{
    async Task Because() => await ExecuteWith(new BFilter(), new AFilter());

    [Fact] void should_report_the_error_from_the_filter_sorting_first() => _result.ValidationResults.Select(_ => _.Message).ShouldContainOnly("error from A");
}
