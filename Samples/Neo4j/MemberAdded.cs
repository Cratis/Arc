// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Samples.Neo4j;

/// <summary>
/// The event that a person was added to a group. The graph projection of <see cref="MemberOf"/> turns it into an edge.
/// </summary>
/// <param name="PersonId">The person.</param>
/// <param name="GroupId">The group.</param>
[EventType]
public record MemberAdded(Guid PersonId, Guid GroupId);
