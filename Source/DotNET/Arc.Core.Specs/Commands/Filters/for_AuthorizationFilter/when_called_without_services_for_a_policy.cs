// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Execution;

namespace Cratis.Arc.Commands.Filters.for_AuthorizationFilter;

public class when_called_without_services_for_a_policy : Specification
{
    CommandResult _result;

    async Task Because()
    {
        var allow = Substitute.For<IAuthorizationEvaluator>();
        allow.IsAuthorized(typeof(ProtectedCommand)).Returns(true);
        var context = new CommandContext(CorrelationId.New(), typeof(ProtectedCommand), new ProtectedCommand(), [], new CommandContextValues());
        _result = await new AuthorizationFilter(allow).OnExecution(context);
    }

    [Fact] void should_not_accept_the_legacy_allow_without_a_policy_runtime() => _result.IsAuthorized.ShouldBeFalse();

    [Authorize(Policy = "Protected")]
    public record ProtectedCommand;
}
