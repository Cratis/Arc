// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.EntityFrameworkCore.for_DbContextServiceCollectionExtensions;

public record TestId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly TestId NotSet = new(Guid.Empty);
    public static implicit operator TestId(Guid value) => new(value);
}
