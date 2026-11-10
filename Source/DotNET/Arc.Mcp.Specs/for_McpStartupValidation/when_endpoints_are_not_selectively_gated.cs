// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpStartupValidation.given;

namespace Cratis.Arc.Mcp.for_McpStartupValidation;

public class when_endpoints_are_not_selectively_gated : a_startup_validation
{
    void Establish() => _withGatedEndpoints = false;

    void Because() => Validate();

    [Fact] void should_refuse_startup() => _error.ShouldBeOfExactType<McpRequiresGatedEndpoints>();
    [Fact] void should_name_capability_hosting() => _error!.Message.ShouldContain("WithCapabilities");
    [Fact] void should_name_selective_gating() => _error!.Message.ShouldContain("CapabilitiesOptions.GatedEndpoints");
    [Fact] void should_name_verified_callers() => _error!.Message.ShouldContain("WithVerifiedCallers");
    [Fact] void should_link_to_troubleshooting() => _error!.Message.ShouldContain("https://www.cratis.io/arc/backend/csharp/mcp/troubleshooting");
    [Fact] void should_not_require_registration_order() => _error!.Message.Contains("before WithMcp", StringComparison.Ordinal).ShouldBeFalse();
}
