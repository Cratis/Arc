// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandActionFilter.when_executing;

public class and_the_action_is_an_asp_net_result : given.a_post_request_with_execution_scopes
{
    bool _executed;

    void Establish() => _actionContext.Filters.Add(new AspNetResultAttribute());

    async Task Because() => await _filter.OnActionExecutionAsync(_actionContext, () =>
    {
        _executed = true;
        return Task.FromResult(_executedContext);
    });

    [Fact] void should_execute_the_action() => _executed.ShouldBeTrue();
    [Fact] void should_not_run_any_scopes() => _calls.ShouldBeEmpty();
    [Fact] void should_keep_the_asp_net_result() => ((Microsoft.AspNetCore.Mvc.ObjectResult)_executedContext.Result!).Value.ShouldEqual("response");
}
