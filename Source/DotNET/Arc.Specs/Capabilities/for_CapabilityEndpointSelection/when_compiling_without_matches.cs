// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using static Cratis.Arc.Capabilities.for_CapabilityEndpointSelection.given.endpoints;

namespace Cratis.Arc.Capabilities.for_CapabilityEndpointSelection;

public class when_compiling_without_matches : Specification
{
    Exception? _empty;
    Exception? _noSources;
    Exception? _noMatches;
    Exception? _differentCase;

    void Because()
    {
        var options = new CapabilitiesOptions { GatedEndpoints = new CapabilityEndpointSelector() };
        _empty = Catch.Exception(new CapabilityEndpointSelection(Options.Create(options)).Validate);
        options.GatedEndpoints.ForRoutePattern("/mcp-spec");
        _noSources = Catch.Exception(() => new CapabilityEndpointSelection(Options.Create(options)).Compile([]));
        _noMatches = Catch.Exception(() => new CapabilityEndpointSelection(Options.Create(options)).Compile([new DefaultEndpointDataSource(Route("/legacy"))]));
        _differentCase = Catch.Exception(() => new CapabilityEndpointSelection(Options.Create(options)).Compile([new DefaultEndpointDataSource(Route("/MCP-SPEC"))]));
    }

    [Fact] void should_refuse_an_explicit_empty_selector() => _empty.ShouldBeOfExactType<CapabilityEndpointSelectorMatchesNothing>();
    [Fact] void should_refuse_without_data_sources() => _noSources.ShouldBeOfExactType<CapabilityEndpointSelectorMatchesNothing>();
    [Fact] void should_refuse_without_selected_endpoints() => _noMatches.ShouldBeOfExactType<CapabilityEndpointSelectorMatchesNothing>();
    [Fact] void should_match_route_pattern_text_exactly() => _differentCase.ShouldBeOfExactType<CapabilityEndpointSelectorMatchesNothing>();
}
