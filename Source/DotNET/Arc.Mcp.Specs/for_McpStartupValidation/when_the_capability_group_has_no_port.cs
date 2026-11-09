// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Capabilities;
using Cratis.Arc.Mcp.for_McpStartupValidation.given;

namespace Cratis.Arc.Mcp.for_McpStartupValidation;

public class when_the_capability_group_has_no_port : a_startup_validation
{
    void Establish() => _arc = new ArcOptions();

    void Because() => Validate();

    [Fact] void should_refuse_startup() => _error.ShouldBeOfExactType<CapabilityEndpointGroupHasNoPort>();
}
