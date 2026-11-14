// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.Graphs;
using Cratis.Chronicle.Projections.ModelBound;

namespace Samples.Neo4j;

/// <summary>
/// The edge from a person to a group they are a member of.
/// </summary>
/// <param name="PersonId">The key of the person node.</param>
/// <param name="GroupId">The key of the group node.</param>
/// <param name="Since">When the person joined.</param>
[Graph("Organization")]
[Edge("MEMBER_OF")]
[EdgeFrom<MemberAdded>(sourceKey: nameof(MemberAdded.PersonId), targetKey: nameof(MemberAdded.GroupId))]
public record MemberOf(
    [Source<Person>] Guid PersonId,
    [Target<Group>] Guid GroupId,
    [SetFromContext<MemberAdded>(nameof(EventContext.Occurred))] DateTimeOffset Since);
