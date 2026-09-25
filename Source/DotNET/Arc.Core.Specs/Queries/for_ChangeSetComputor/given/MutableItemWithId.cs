// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ChangeSetComputor.given;

/// <summary>
/// An item type that overrides neither <see cref="object.Equals(object)"/> nor equality operators, so comparing two
/// snapshots of it has to fall back to serializing them.
/// </summary>
public class MutableItemWithId
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}
