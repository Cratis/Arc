// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Capabilities;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Represents an admitted call of a tool from the ceiling.
/// </summary>
/// <param name="Tool">The tool.</param>
/// <param name="Caller">The verified caller.</param>
/// <param name="Arguments">The arguments the client sent.</param>
/// <param name="Services">The services of the request.</param>
/// <param name="CancellationToken">The cancellation token.</param>
internal sealed record McpOperationCall(
    McpToolDescriptor Tool,
    CapabilityCaller Caller,
    IReadOnlyDictionary<string, JsonElement> Arguments,
    IServiceProvider Services,
    CancellationToken CancellationToken)
{
    /// <summary>
    /// Gets the MCP protocol request the call arrived in, if any.
    /// </summary>
    internal RequestContext<CallToolRequestParams>? Request { get; init; }
}
