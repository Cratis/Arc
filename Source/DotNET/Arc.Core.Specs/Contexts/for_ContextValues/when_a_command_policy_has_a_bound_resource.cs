// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;

namespace Cratis.Arc.Contexts.for_ContextValues;

public class when_a_command_policy_has_a_bound_resource : given.an_execution_source
{
    object _value;
    object _withoutInstance;

    async Task Because()
    {
        await new Commands.Filters.AvailabilityFilter().OnExecution(_command);
        var context = new AuthorizationPolicyContext(new ClaimsPrincipal(), typeof(Command), _command) { Services = _services };
        _value = await context.ProvidedValues.GetAsync("value");
        _withoutInstance = await (context with { Resource = new object() }).ProvidedValues.GetAsync("value");
    }

    [Fact] void should_read_the_bound_value() => _value.ShouldEqual("bound");
    [Fact] void should_reuse_the_gates_exact_invocation() => _reads.Single().ShouldBeSame(_gated);
    [Fact] void should_keep_the_command_instance() => _reads.Single().Arguments.ShouldBeSame(_command.Command);
    [Fact] void should_keep_the_pinned_receipt() => _reads.Single().ReceivedAt.ShouldEqual(Receipt);
    [Fact] void should_not_infer_an_instance_from_a_shared_service_scope() => _withoutInstance.ShouldBeNull();
}
