// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpStartupValidation.given;

namespace Cratis.Arc.Mcp.for_McpStartupValidation;

public class when_the_configuration_is_valid : a_startup_validation
{
    void Because() => Validate();

    [Fact] void should_allow_startup_with_nothing_exposed() => _error.ShouldBeNull();
}
