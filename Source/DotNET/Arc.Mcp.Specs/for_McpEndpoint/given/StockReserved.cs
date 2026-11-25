// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>
/// Records the command's decision based on the protected stock read.
/// </summary>
/// <param name="Available">The availability observed by the handler.</param>
[EventType]
public record StockReserved(int Available);
