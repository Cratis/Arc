// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using static Cratis.Arc.Capabilities.for_CapabilityEndpointSelection.given.endpoints;

namespace Cratis.Arc.Capabilities.for_CapabilityEndpointSelection;

public class when_compiling_a_selection : Specification
{
    CapabilityEndpointSelection _selection;
    Endpoint _selected;
    Endpoint _alias;
    Endpoint _outside;
    Endpoint _unknown;
    Endpoint _notRouted;
    bool _outsideBeforeCompile;

    void Establish()
    {
        _selected = Route("/mcp-spec", new Marker());
        _alias = Route("/alias/mcp", new Marker());
        _outside = Route("/legacy/{id}");
        _unknown = Route("/legacy/{id}");
        _notRouted = new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "method refusal");
        _selection = new(Options.Create(new CapabilitiesOptions
        {
            GatedEndpoints = new CapabilityEndpointSelector().ForRoutePattern("/mcp-spec").ForEndpointMetadata<Marker>()
        }));
        _outsideBeforeCompile = _selection.IsOutside(_outside);
    }

    void Because() => _selection.Compile([new DefaultEndpointDataSource(_selected, _alias, _outside, _notRouted)]);

    [Fact] void should_keep_the_selected_route_gated() => _selection.IsOutside(_selected).ShouldBeFalse();
    [Fact] void should_keep_metadata_aliases_gated() => _selection.IsOutside(_alias).ShouldBeFalse();
    [Fact] void should_allow_known_outside_routes() => _selection.IsOutside(_outside).ShouldBeTrue();
    [Fact] void should_gate_before_compilation() => _outsideBeforeCompile.ShouldBeFalse();
    [Fact] void should_gate_unknown_endpoint_instances() => _selection.IsOutside(_unknown).ShouldBeFalse();
    [Fact] void should_gate_without_an_endpoint() => _selection.IsOutside(null).ShouldBeFalse();
    [Fact] void should_gate_non_route_endpoints() => _selection.IsOutside(_notRouted).ShouldBeFalse();
}
