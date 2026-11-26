// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_the_same_envelope_is_decided_in_different_service_scopes : given.an_invocation_host
{
    AvailabilityDecision _first;
    AvailabilityDecision _second;

    protected override void Configure(ArcAvailabilityOptions options) => options.Gate<UngatedCommand>(AvailabilityExpression.When(
        new ContextComparison("tenant", ComparisonOperator.Equal, "acme")));

    async Task Because()
    {
        var invocation = new AvailabilityInvocation(typeof(UngatedCommand), new { VesselId = "allowed" }, Receipt);
        var gate = (IAvailabilityExecutionGate)_gate;
        _first = await gate.Decide(invocation, _scope.ServiceProvider, CancellationToken.None);
        using var otherScope = _root.CreateScope();
        _root.GetRequiredService<ITenantIdAccessor>().Current.Returns(new TenantId("other"));
        _second = await gate.Decide(invocation, otherScope.ServiceProvider, CancellationToken.None);
    }

    [Fact] void should_use_the_first_scopes_subject() => _first.IsAvailable.ShouldBeTrue();
    [Fact] void should_not_reuse_it_in_another_scope() => _second.IsAvailable.ShouldBeFalse();
}
