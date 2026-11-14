// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Samples.Neo4j;

/// <summary>
/// The event that a person was registered. The graph projection of <see cref="Person"/> turns it into a node.
/// </summary>
/// <param name="PersonId">The identity of the person.</param>
/// <param name="Name">The name of the person.</param>
[EventType]
public record PersonRegistered(Guid PersonId, string Name);
