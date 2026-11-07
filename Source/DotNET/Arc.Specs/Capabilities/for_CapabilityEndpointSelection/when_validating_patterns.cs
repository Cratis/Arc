// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Capabilities.for_CapabilityEndpointSelection;

public class when_validating_patterns
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData(null)]
    public void should_refuse_blank_patterns(string? pattern) =>
        Catch.Exception(() => new CapabilityEndpointSelector().ForRoutePattern(pattern!)).ShouldBeOfExactType<ArgumentException>();

    [Theory]
    [InlineData("/mcp", "/mcp")]
    [InlineData("/mcp", "/MCP")]
    public void should_refuse_duplicate_patterns(string first, string second)
    {
        var selection = new CapabilityEndpointSelection(Options.Create(new CapabilitiesOptions
        {
            GatedEndpoints = new CapabilityEndpointSelector().ForRoutePattern(first).ForRoutePattern(second)
        }));
        Catch.Exception(selection.Validate).ShouldBeOfExactType<CapabilityEndpointSelectorIsAmbiguous>();
    }
}
