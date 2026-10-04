// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries;

/// <summary>
/// Decides whether a client applying a <see cref="ChangeSet"/> ends up with the items in the order the server has them.
/// </summary>
/// <remarks>
/// A client applies a change set by dropping the removed items, replacing changed items where they stand and
/// appending the added items at the end. That keeps every item, but not their order: an item that moved, or one
/// added anywhere but last, ends up in the wrong place. For a query whose order means something - it is sorted or
/// paged - such a delta has to be sent as a full snapshot instead.
/// </remarks>
internal static class ChangeSetOrder
{
    /// <summary>
    /// Checks whether applying the change set to the previous items, the way a client does, reproduces the current order.
    /// </summary>
    /// <param name="changeSet">The <see cref="ChangeSet"/> the client would apply.</param>
    /// <param name="previous">The items the client holds.</param>
    /// <param name="current">The items, in order, the server holds now.</param>
    /// <param name="idProperty">The property carrying the identity of an item.</param>
    /// <returns>True when the client ends up with the current items in the current order.</returns>
    public static bool IsReproducedBy(ChangeSet changeSet, IEnumerable<object> previous, IEnumerable<object> current, PropertyInfo idProperty)
    {
        var removed = changeSet.Removed.Select(idProperty.GetValue).ToHashSet();
        var applied = previous
            .Select(idProperty.GetValue)
            .Where(id => !removed.Contains(id))
            .Concat(changeSet.Added.Select(idProperty.GetValue));

        return applied.SequenceEqual(current.Select(idProperty.GetValue));
    }
}
