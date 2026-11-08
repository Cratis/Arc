// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.Capabilities;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>
/// Records every operation the host dispatched, with the caller it ran as.
/// </summary>
public class McpProbe
{
    /// <summary>
    /// Gets the dispatched operations.
    /// </summary>
    public ConcurrentQueue<(string Operation, string? Text, CapabilityCaller? Caller)> Dispatched { get; } = new();

    /// <summary>
    /// Gets the number of dispatched operations.
    /// </summary>
    public int Count => Dispatched.Count;

    /// <summary>
    /// Records a dispatch.
    /// </summary>
    /// <param name="operation">The operation.</param>
    /// <param name="text">The text argument, if any.</param>
    /// <param name="caller">The caller it ran as.</param>
    public void Record(string operation, string? text, CapabilityCaller? caller) => Dispatched.Enqueue((operation, text, caller));
}
