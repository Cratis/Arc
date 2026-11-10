// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpStartupValidation.given;

namespace Cratis.Arc.Mcp.for_McpStartupValidation;

public class when_capabilities_are_not_enabled : a_startup_validation
{
    void Establish() => _withCapabilities = false;

    void Because() => Validate();

    [Fact] void should_refuse_startup() => _error.ShouldBeOfExactType<McpRequiresCapabilities>();
    [Fact] void should_name_capability_hosting() => _error!.Message.ShouldContain("WithCapabilities");
    [Fact] void should_name_the_endpoint_group_port() => _error!.Message.ShouldContain("port assigned to its endpoint group");
    [Fact] void should_name_verified_callers() => _error!.Message.ShouldContain("WithPersonalAccessTokens");
    [Fact] void should_link_to_troubleshooting() => _error!.Message.ShouldContain("https://www.cratis.io/arc/backend/csharp/mcp/troubleshooting");
    [Fact] void should_not_require_registration_order() => _error!.Message.Contains("before WithMcp", StringComparison.Ordinal).ShouldBeFalse();
}
