// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.EntityFrameworkCore.for_ReadOnlyDbContextExtensions;

public record TestReadOnlyId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly TestReadOnlyId NotSet = new(Guid.Empty);
    public static implicit operator TestReadOnlyId(Guid value) => new(value);
}
