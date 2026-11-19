// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>
/// Records the available stock used by a protected decision.
/// </summary>
/// <param name="Available">The available stock.</param>
[EventType]
public record StockChanged(int Available);
