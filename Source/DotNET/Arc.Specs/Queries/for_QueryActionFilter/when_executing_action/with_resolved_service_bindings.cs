// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Cratis.Arc.Queries.for_QueryActionFilter.when_executing_action;

public class with_resolved_service_bindings : given.a_query_with_injected_parameters
{
    void Establish()
    {
        _actionContext.ActionDescriptor.Parameters.Single(parameter => parameter.Name == "input").BindingInfo =
            new() { BindingSource = BindingSource.Services };
        _actionContext.ActionDescriptor.Parameters.Single(parameter => parameter.Name == "keyed").BindingInfo =
            new() { ServiceKey = "query" };
    }

    async Task Because() => await _filter.OnActionExecutionAsync(_actionContext, _next);

    [Fact] void should_exclude_the_resolved_service_parameter() => _context.Arguments.ContainsKey("input").ShouldBeFalse();
    [Fact] void should_exclude_the_resolved_keyed_service() => _context.Arguments.ContainsKey("keyed").ShouldBeFalse();
}
