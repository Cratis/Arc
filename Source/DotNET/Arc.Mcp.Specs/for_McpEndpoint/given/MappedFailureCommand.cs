// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>A command that fails before returning a response.</summary>
public record MappedFailureCommand
{
    /// <summary>Fails the command.</summary>
    /// <exception cref="InvalidOperationException">Always fails for the mapping specification.</exception>
    public void Handle() => throw new InvalidOperationException("private failure detail");
}
