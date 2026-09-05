// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a block removing a read model instance when an event occurs.
/// </summary>
/// <param name="EventType">The identifier of the event type triggering the removal.</param>
/// <param name="Key">The key expression, if the block declares one.</param>
/// <param name="ParentKey">The parent key expression, if the block declares one.</param>
public record ProjectionRemoveModel(string EventType, string? Key, string? ParentKey);
