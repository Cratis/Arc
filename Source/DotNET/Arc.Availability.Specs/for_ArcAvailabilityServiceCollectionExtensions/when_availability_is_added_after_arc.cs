// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability.for_ArcAvailabilityServiceCollectionExtensions;

public class when_availability_is_added_after_arc : Specification
{
    ServiceProvider _root;
    IServiceScope _first;
    IServiceScope _second;
    IAvailabilityGate _gate;
    ContextScope _scope;

    void Because()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCratisArcCore();
        services.AddArcAvailability(options =>
        {
            options.Gate<given.an_availability_host.UngatedCommand>(AvailabilityExpression.For("Plus"));
            options.ScopeFor = _ => new ContextScope(["Tenant"]);
        });

        _root = services.BuildServiceProvider(true);
        _first = _root.CreateScope();
        _second = _root.CreateScope();
        _gate = _root.GetRequiredService<IAvailabilityGate>();
        _scope = _root.GetRequiredService<ArcAvailabilityOptions>().ScopeFor(typeof(string));
    }

    void Destroy()
    {
        _first.Dispose();
        _second.Dispose();
        _root.Dispose();
    }

    [Fact] void should_gate_the_registered_part() => _gate.IsGated(typeof(given.an_availability_host.UngatedCommand)).ShouldBeTrue();
    [Fact] void should_use_the_configured_scope() => _scope.ShouldNotEqual(ContextScope.Application);
    [Fact] void should_share_the_options_between_resolutions() => _root.GetRequiredService<ArcAvailabilityOptions>().ShouldEqual(_root.GetRequiredService<ArcAvailabilityOptions>());
    [Fact] void should_share_the_request_context_within_a_scope() => _first.ServiceProvider.GetRequiredService<AvailabilityRequestContext>().ShouldEqual(_first.ServiceProvider.GetRequiredService<AvailabilityRequestContext>());
    [Fact] void should_not_share_the_request_context_between_scopes() => _first.ServiceProvider.GetRequiredService<AvailabilityRequestContext>().ShouldNotEqual(_second.ServiceProvider.GetRequiredService<AvailabilityRequestContext>());
}
