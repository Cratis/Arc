// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// Marks the endpoints that serve MCP. The capability gate selects them for admission and refuses them with MCP challenges.
/// </summary>
public sealed class McpEndpointMetadata
{
    McpEndpointMetadata()
    {
    }

    /// <summary>
    /// Gets the shared instance.
    /// </summary>
    public static McpEndpointMetadata Instance { get; } = new();
}
