// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.Filters;
using Cratis.Arc.Testing.Commands;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_built_in_filters_are_discovered : given.a_discovered_filter_evaluation
{
    bool _pipelineAuthorized;
    async Task Because()
    {
        Result = await CommandAuthorization.For<given.RoleCommand>(Provider, Principal);
        await using var scenario = new CommandScenario<given.RoleCommand>();
        foreach (var descriptor in Services) scenario.Services.Add(descriptor);
        scenario.WithPrincipal(Principal);
        _pipelineAuthorized = (await scenario.Execute(new given.RoleCommand())).IsAuthorized;
    }

    [Fact] void should_authorize_without_a_gate() => Result.ShouldBeAuthorized();
    [Fact] void should_match_the_pipeline() => _pipelineAuthorized.ShouldBeTrue();
    [Fact] void should_use_real_discovery() => Provider.GetRequiredService<IInstancesOf<ICommandFilter>>().GetType().ShouldNotEqual(typeof(KnownInstancesOf<ICommandFilter>));
    [Fact] void should_discover_the_real_availability_filter() => Provider.GetRequiredService<IInstancesOf<ICommandFilter>>().OfType<AvailabilityFilter>().Count().ShouldEqual(1);
    [Fact] void should_discover_all_core_filters() => Provider.GetRequiredService<IInstancesOf<ICommandFilter>>().Count().ShouldEqual(4);
}
