// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Capabilities;

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
    CancellationToken CancellationToken);
