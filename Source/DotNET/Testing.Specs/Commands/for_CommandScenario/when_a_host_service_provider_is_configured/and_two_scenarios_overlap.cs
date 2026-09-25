// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.for_CommandScenario.when_a_host_service_provider_is_configured;

/// <summary>
/// Two scenarios alive at the same time must not share state through the process-wide provider: disposing one
/// leaves the other able to validate and execute, and neither replaces the host's provider.
/// </summary>
[Collection(GlobalServiceProviderCollection.Name)]
public class and_two_scenarios_overlap : given.a_host_service_provider
{
    CommandScenario<NamedWork> _first;
    CommandScenario<NamedWork> _second;
    CommandResult _rejected;
    CommandResult _accepted;

    async Task Establish()
    {
        _first = new CommandScenario<NamedWork>();
        _second = new CommandScenario<NamedWork>();
        await _first.Execute(new NamedWork("First"));
        await _second.Execute(new NamedWork("Second"));
    }

    async Task Because()
    {
        await _first.DisposeAsync();
        _rejected = await _second.Execute(new NamedWork(string.Empty));
        _accepted = await _second.Execute(new NamedWork("Still running"));
        await _second.DisposeAsync();
    }

    [Fact] void should_reject_an_invalid_command_in_the_remaining_scenario() => _rejected.IsValid.ShouldBeFalse();
    [Fact] void should_execute_a_valid_command_in_the_remaining_scenario() => _accepted.IsSuccess.ShouldBeTrue();
    [Fact] void should_leave_the_host_service_provider_in_place() => Internals.ServiceProvider.ShouldBeSame(_hostServiceProvider);
}
