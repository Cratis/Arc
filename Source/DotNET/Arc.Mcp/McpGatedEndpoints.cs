// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Capabilities;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Makes the capability gate verify every MCP request: the MCP endpoint is added to an explicit selection, and without
/// a selection the whole capability port stays gated, after routing so the gate refuses in the MCP shape.
/// </summary>
internal sealed class McpGatedEndpoints : IPostConfigureOptions<CapabilitiesOptions>
{
    /// <inheritdoc/>
    public void PostConfigure(string? name, CapabilitiesOptions options)
    {
        if (options.GatedEndpoints is not { } selector)
        {
            options.GateAfterRouting = true;
            return;
        }

        if (!selector.HasEndpointMetadata<McpEndpointMetadata>())
        {
            selector.ForEndpointMetadata<McpEndpointMetadata>();
        }
    }
}
