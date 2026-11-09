// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Capabilities;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Adds the MCP endpoint to the selectively gated endpoints, so the capability gate verifies every MCP request.
/// </summary>
internal sealed class McpGatedEndpoints : IPostConfigureOptions<CapabilitiesOptions>
{
    /// <inheritdoc/>
    public void PostConfigure(string? name, CapabilitiesOptions options)
    {
        if (options.GatedEndpoints is { } selector && !selector.HasEndpointMetadata<McpEndpointMetadata>())
        {
            selector.ForEndpointMetadata<McpEndpointMetadata>();
        }
    }
}
