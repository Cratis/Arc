// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpStartupValidation.given;

namespace Cratis.Arc.Mcp.for_McpStartupValidation;

public class when_endpoints_are_not_selectively_gated : a_startup_validation
{
    void Establish() => _withGatedEndpoints = false;

    void Because() => Validate();

    [Fact] void should_refuse_startup() => _error.ShouldBeOfExactType<McpRequiresGatedEndpoints>();
}
