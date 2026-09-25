// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents one item-level change an observable collection query already knows it made.
/// </summary>
/// <param name="Kind">What happened to the item.</param>
/// <param name="Id">The identity of the item it happened to.</param>
/// <remarks>
/// A provider that watches a data source is told exactly which item changed. Carrying that identity alongside the
/// emission is what lets the delta for the emission be stated rather than rediscovered by comparing the whole new
/// snapshot against the whole previous one.
/// </remarks>
public record CollectionChange(CollectionChangeKind Kind, object Id);
