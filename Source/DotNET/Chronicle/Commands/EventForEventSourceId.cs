// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Represents an event paired with its specific event source id.
/// </summary>
/// <param name="EventSourceId">The event source id to append the event to.</param>
/// <param name="Event">The event to append.</param>
public record EventForEventSourceId(EventSourceId EventSourceId, object Event);
