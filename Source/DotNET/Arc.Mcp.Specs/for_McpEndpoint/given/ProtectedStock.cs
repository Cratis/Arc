// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>
/// A foldable decision model for the MCP command host.
/// </summary>
/// <param name="Id">The stock identity.</param>
/// <param name="Available">The available stock.</param>
[ReadModel, FromEvent<StockChanged>]
public record ProtectedStock([property: Key] EventSourceId Id, int Available);
