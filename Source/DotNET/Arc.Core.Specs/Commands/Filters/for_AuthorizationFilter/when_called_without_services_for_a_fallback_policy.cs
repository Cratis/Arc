// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Execution;

namespace Cratis.Arc.Commands.Filters.for_AuthorizationFilter;

public class when_called_without_services_for_a_fallback_policy : Specification
{
    CommandResult _result;

    async Task Because()
    {
        var evaluator = Substitute.For<IAuthorizationEvaluator>();
        evaluator.IsAuthorized(typeof(UnannotatedCommand)).Returns(_ => throw new AsynchronousAuthorizationRequired());
        var context = new CommandContext(CorrelationId.New(), typeof(UnannotatedCommand), new UnannotatedCommand(), [], new CommandContextValues());
        _result = await new AuthorizationFilter(evaluator).OnExecution(context);
    }

    [Fact] void should_fail_closed_when_the_fallback_requires_async_authorization() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_return_an_authorization_result_without_exceptions() => _result.ExceptionMessages.ShouldBeEmpty();

    public record UnannotatedCommand;
}
