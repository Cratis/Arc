// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Describes what happened to a single item of an observed collection.
/// </summary>
public enum CollectionChangeKind
{
    /// <summary>
    /// The item entered the observed set.
    /// </summary>
    Added = 0,

    /// <summary>
    /// The item was already in the observed set and its content changed.
    /// </summary>
    Replaced = 1,

    /// <summary>
    /// The item left the observed set, either because it was removed or because it no longer matches the filter.
    /// </summary>
    Removed = 2
}
