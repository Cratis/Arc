// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// The events a <see cref="ReadModelFolds"/> fold materializes a read model from.
/// </summary>
internal abstract record ReadModelFoldInput
{
    /// <summary>
    /// Events read from a scenario's event log, keeping their original contexts.
    /// </summary>
    /// <param name="Events">The appended events.</param>
    internal sealed record Appended(IEnumerable<AppendedEvent> Events) : ReadModelFoldInput;

    /// <summary>
    /// Bare events seeded for one event source.
    /// </summary>
    /// <param name="EventSourceId">The event source the events happened for.</param>
    /// <param name="Events">The seeded events.</param>
    internal sealed record Seeded(EventSourceId EventSourceId, IEnumerable<object> Events) : ReadModelFoldInput;
}
