// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using static Cratis.Arc.Capabilities.for_CapabilityEndpointSelection.given.endpoints;

namespace Cratis.Arc.Capabilities.for_CapabilityOutsideEndpointFeature;

public class when_reassigning_an_outside_endpoint : Specification
{
    Endpoint _first;
    Endpoint _second;
    Endpoint _selected;
    Endpoint _refusal;
    CapabilityEndpointSelection _selection;
    Endpoint? _allowed;
    Endpoint? _clearedThenAllowed;
    Endpoint? _refusedSelected;
    Endpoint? _refusedReplacement;
    Endpoint? _latchedRefusal;
    Endpoint? _clearedRefusal;

    void Establish()
    {
        _first = Route("/legacy/first");
        _second = Route("/legacy/second");
        _selected = Route("/mcp-spec");
        _refusal = new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "refusal");
        _selection = new CapabilityEndpointSelection(Options.Create(new CapabilitiesOptions
        {
            GatedEndpoints = new CapabilityEndpointSelector().ForRoutePattern("/mcp-spec")
        }));
        _selection.Compile([new DefaultEndpointDataSource(_first, _second, _selected)]);
    }

    void Because()
    {
        var feature = new CapabilityOutsideEndpointFeature(_selection, _first, _refusal)
        {
            Endpoint = _second
        };
        _allowed = feature.Endpoint;
        feature.Endpoint = null;
        feature.Endpoint = _first;
        _clearedThenAllowed = feature.Endpoint;
        feature.Endpoint = _selected;
        _refusedSelected = feature.Endpoint;
        feature.Endpoint = _second;
        _latchedRefusal = feature.Endpoint;
        feature.Endpoint = null;
        _clearedRefusal = feature.Endpoint;
        var replaced = new CapabilityOutsideEndpointFeature(_selection, _first, _refusal)
        {
            Endpoint = Route("/legacy/second")
        };
        _refusedReplacement = replaced.Endpoint;
    }

    [Fact] void should_allow_another_exact_known_outside_instance() => _allowed.ShouldEqual(_second);
    [Fact] void should_allow_clearing_before_selecting_a_known_outside_instance() => _clearedThenAllowed.ShouldEqual(_first);
    [Fact] void should_replace_a_selected_endpoint_with_refusal() => _refusedSelected.ShouldEqual(_refusal);
    [Fact] void should_refuse_a_new_instance_even_for_an_outside_route() => _refusedReplacement.ShouldEqual(_refusal);
    [Fact] void should_not_restore_outside_admission_after_refusal() => _latchedRefusal.ShouldEqual(_refusal);
    [Fact] void should_not_clear_a_refusal() => _clearedRefusal.ShouldEqual(_refusal);
}
