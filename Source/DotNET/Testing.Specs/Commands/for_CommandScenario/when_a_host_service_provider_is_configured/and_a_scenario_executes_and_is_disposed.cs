// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.for_CommandScenario.when_a_host_service_provider_is_configured;

/// <summary>
/// A scenario keeps its service provider to itself: executing and disposing it must leave the host's
/// process-wide provider in place rather than a provider the scenario has already disposed.
/// </summary>
[Collection(GlobalServiceProviderCollection.Name)]
public class and_a_scenario_executes_and_is_disposed : given.a_host_service_provider
{
    CommandResult _result;

    async Task Because()
    {
        await using var scenario = new CommandScenario<NamedWork>();
        _result = await scenario.Execute(new NamedWork("Work"));
    }

    [Fact] void should_execute_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_leave_the_host_service_provider_in_place() => Internals.ServiceProvider.ShouldBeSame(_hostServiceProvider);
}
