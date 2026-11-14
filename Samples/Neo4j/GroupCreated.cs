// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Samples.Neo4j;

/// <summary>
/// The event that a group was created. The graph projection of <see cref="Group"/> turns it into a node.
/// </summary>
/// <param name="GroupId">The identity of the group.</param>
/// <param name="Name">The name of the group.</param>
[EventType]
public record GroupCreated(Guid GroupId, string Name);
