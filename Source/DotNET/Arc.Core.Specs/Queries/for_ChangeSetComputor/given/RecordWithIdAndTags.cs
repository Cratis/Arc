// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ChangeSetComputor.given;

/// <summary>
/// A record whose value equality compares its collection by reference, so two equal-looking snapshots are not equal.
/// </summary>
/// <param name="Id">The identity of the item.</param>
/// <param name="Tags">The tags of the item, compared by reference by the record equality.</param>
public record RecordWithIdAndTags(Guid Id, IReadOnlyList<string> Tags);
