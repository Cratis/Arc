// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryActionFilter.when_executing_action;

public class with_only_injected_parameters : given.a_query_with_injected_parameters
{
    void Establish() => _actionContext.ActionDescriptor.Parameters = _actionContext.ActionDescriptor.Parameters
        .Where(parameter => parameter.Name is not "count" and not "id" and not "input").ToList();

    async Task Because() => await _filter.OnActionExecutionAsync(_actionContext, _next);

    [Fact] void should_have_no_caller_arguments() => _context.Arguments.ShouldBeEmpty();
}
