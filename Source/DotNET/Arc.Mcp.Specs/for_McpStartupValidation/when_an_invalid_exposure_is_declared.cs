// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Mcp.for_McpStartupValidation.given;

namespace Cratis.Arc.Mcp.for_McpStartupValidation;

public class when_an_invalid_exposure_is_declared : a_startup_validation
{
    void Establish() => _mcp = mcp => mcp.Configure(_ => _.ApplicationName = "app").Expose<InvalidlyExposed>("Not Valid", "Described.");

    void Because() => Validate();

    [Fact] void should_refuse_startup() => _error.ShouldBeOfExactType<McpToolNameInvalid>();

    sealed record InvalidlyExposed;
}
