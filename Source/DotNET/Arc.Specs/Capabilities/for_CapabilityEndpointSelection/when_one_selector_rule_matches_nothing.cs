// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using static Cratis.Arc.Capabilities.for_CapabilityEndpointSelection.given.endpoints;

namespace Cratis.Arc.Capabilities.for_CapabilityEndpointSelection;

public class when_one_selector_rule_matches_nothing : Specification
{
    Exception? _mistypedPattern;
    Exception? _missingMetadata;
    Exception? _secondMissingMetadata;

    void Because()
    {
        _mistypedPattern = Compile(new CapabilityEndpointSelector().ForRoutePattern("/mcp-spec").ForRoutePattern("/mcp-sepc"));
        _missingMetadata = Compile(new CapabilityEndpointSelector().ForRoutePattern("/mcp-spec").ForEndpointMetadata<HttpMethodMetadata>());
        _secondMissingMetadata = Compile(new CapabilityEndpointSelector().ForEndpointMetadata<Marker>().ForEndpointMetadata<HttpMethodMetadata>());
    }

    static Exception? Compile(CapabilityEndpointSelector selector) => Catch.Exception(() =>
        new CapabilityEndpointSelection(Options.Create(new CapabilitiesOptions { GatedEndpoints = selector }))
            .Compile([new DefaultEndpointDataSource(Route("/mcp-spec", new Marker()), Route("/legacy"))]));

    [Fact] void should_refuse_a_second_mistyped_pattern() => _mistypedPattern.ShouldBeOfExactType<CapabilityEndpointSelectorMatchesNothing>();
    [Fact] void should_refuse_an_unmatched_metadata_rule_even_when_a_pattern_matches() => _missingMetadata.ShouldBeOfExactType<CapabilityEndpointSelectorMatchesNothing>();
    [Fact] void should_refuse_a_second_metadata_type_matching_nothing() => _secondMissingMetadata.ShouldBeOfExactType<CapabilityEndpointSelectorMatchesNothing>();
}
