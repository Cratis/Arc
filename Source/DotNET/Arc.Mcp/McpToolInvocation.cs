// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Capabilities;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Represents a call of a curated tool.
/// </summary>
/// <param name="Tool">The tool from the ceiling being called.</param>
/// <param name="Caller">The verified caller, taken from the request and never from the arguments.</param>
/// <param name="Arguments">The arguments the client sent.</param>
/// <param name="Services">The services of the request the call belongs to.</param>
/// <param name="CancellationToken">The cancellation token for the call.</param>
public sealed record McpToolInvocation(
    McpToolDescriptor Tool,
    CapabilityCaller Caller,
    IReadOnlyDictionary<string, JsonElement> Arguments,
    IServiceProvider Services,
    CancellationToken CancellationToken)
{
    /// <summary>
    /// Gets the MCP protocol request the call arrived in, or null outside one.
    /// </summary>
    /// <remarks>
    /// Use it for protocol features such as progress notifications, logging to the client, sampling or elicitation through
    /// its <c>Server</c>. Identity comes only from <see cref="Caller"/>: never read the caller, tenant or scopes from the
    /// request, and treat its <c>Params</c> as untrusted input.
    /// </remarks>
    public RequestContext<CallToolRequestParams>? Request { get; init; }
}
