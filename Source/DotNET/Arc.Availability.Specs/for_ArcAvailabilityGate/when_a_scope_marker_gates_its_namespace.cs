// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_a_scope_marker_gates_its_namespace : given.a_typed_availability_host
{
    AvailabilityDecision _closed;
    AvailabilityDecision _open;

    async Task Because()
    {
        _closed = await _gate.Decide(typeof(given.GatedNamespace.InsideTheFeature), _scope.ServiceProvider, CancellationToken.None);
        await _audiences.Grant(AudienceLevel.Tenant, "acme", "Plus");
        using var next = _root.CreateScope();
        _open = await _gate.Decide(typeof(given.GatedNamespace.InsideTheFeature), next.ServiceProvider, CancellationToken.None);
    }

    [Fact] void should_close_a_part_in_the_namespace_for_a_caller_without_the_audience() => _closed.Verdict.ShouldEqual(AvailabilityVerdict.Unavailable);
    [Fact] void should_open_it_once_the_audience_is_held() => _open.Verdict.ShouldEqual(AvailabilityVerdict.Available);
    [Fact] void should_not_gate_a_part_outside_the_namespace() => _gate.GatedParts.ShouldNotContain(typeof(given.TypedDeclarations.Foundation));
}
