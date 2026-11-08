// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Capabilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Writes capability gate refusals for MCP endpoints as MCP authorization challenges.
/// </summary>
/// <param name="options">The MCP options.</param>
internal sealed class McpRefusalWriter(IOptions<McpOptions> options) : ICapabilityRefusalWriter
{
    /// <inheritdoc/>
    public Task Write(HttpContext context, CapabilityRefusal refusal) => refusal.StatusCode switch
    {
        StatusCodes.Status400BadRequest => McpChallenge.Write(context, refusal.StatusCode, "invalid_request"),
        StatusCodes.Status403Forbidden => McpChallenge.Write(context, refusal.StatusCode, "forbidden"),
        _ => McpChallenge.Write(context, StatusCodes.Status401Unauthorized, "unauthorized", McpChallenge.Unauthorized(options.Value))
    };
}
