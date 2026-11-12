// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Capabilities;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Represents the question an <see cref="IMcpToolAuthorizer"/> answers: may this caller use this tool.
/// </summary>
/// <param name="Tool">The tool from the catalog.</param>
/// <param name="Caller">The verified caller, taken from the request and never from the arguments.</param>
/// <param name="Services">The services of the request.</param>
/// <param name="CancellationToken">The cancellation token of the request.</param>
public sealed record McpToolAuthorization(
    McpToolDescriptor Tool,
    CapabilityCaller Caller,
    IServiceProvider Services,
    CancellationToken CancellationToken)
{
    /// <summary>
    /// Gets the operation being authorized. Defaults to <see cref="McpToolOperation.Call"/>.
    /// </summary>
    public McpToolOperation Operation { get; init; } = McpToolOperation.Call;
}
