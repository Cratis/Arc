// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Neo4j.given;

public enum MembershipKind
{
    Member = 0,
    Owner = 1
}

public record Membership(PersonId PersonId, PersonName Name, int Age, MembershipKind Kind, IReadOnlyList<PersonName> Aliases);
