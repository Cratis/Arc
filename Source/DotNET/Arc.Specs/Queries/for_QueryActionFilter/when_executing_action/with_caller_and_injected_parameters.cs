// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryActionFilter.when_executing_action;

public class with_caller_and_injected_parameters : given.a_query_with_injected_parameters
{
    async Task Because() => await _filter.OnActionExecutionAsync(_actionContext, _next);

    [Fact] void should_keep_the_bound_query_value() => _context.Arguments["count"].ShouldEqual(42);
    [Fact] void should_keep_the_bound_route_value() => _context.Arguments["id"].ShouldEqual("invoice-42");
    [Fact] void should_exclude_the_from_services_value() => _context.Arguments.ContainsKey("renderer").ShouldBeFalse();
    [Fact] void should_exclude_the_keyed_service_value() => _context.Arguments.ContainsKey("keyed").ShouldBeFalse();
    [Fact] void should_exclude_the_cancellation_token() => _context.Arguments.ContainsKey("cancellationToken").ShouldBeFalse();
    [Fact] void should_exclude_the_inferred_service_value() => _context.Arguments.ContainsKey("dependency").ShouldBeFalse();
    [Fact] void should_exclude_the_special_value() => _context.Arguments.ContainsKey("special").ShouldBeFalse();
    [Fact] void should_keep_only_caller_arguments() => _context.Arguments.Count.ShouldEqual(2);
}
