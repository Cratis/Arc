// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;

namespace Cratis.Arc.Queries;

/// <summary>
/// Computes <see cref="ChangeSet"/> deltas between consecutive snapshots of observable collection queries.
/// </summary>
/// <remarks>
/// <para>
/// When a collection snapshot is received, this computor compares it with the previous snapshot to determine
/// which items were <see cref="ChangeSet.Added"/>, <see cref="ChangeSet.Replaced"/>, or
/// <see cref="ChangeSet.Removed"/> since the last update.
/// </para>
/// <para>
/// Identity-based delta (including <see cref="ChangeSet.Replaced"/> detection) is used when the item type
/// exposes a property conventionally named <c>Id</c> (case-insensitive). Without an identity property, a
/// JSON-hash fallback is used that only surfaces <see cref="ChangeSet.Added"/> and
/// <see cref="ChangeSet.Removed"/>.
/// </para>
/// </remarks>
/// <param name="serializerOptions">The <see cref="JsonSerializerOptions"/> used for item comparison.</param>
public class ChangeSetComputor(JsonSerializerOptions serializerOptions)
{
    /// <summary>
    /// Discovers the property that represents the identity of an item.
    /// </summary>
    /// <remarks>
    /// Looks for a property conventionally named <c>Id</c> (case-insensitive).
    /// </remarks>
    /// <param name="type">The item type to inspect.</param>
    /// <returns>The identity <see cref="PropertyInfo"/>, or <see langword="null"/> if not found.</returns>
    public static PropertyInfo? FindIdentityProperty(Type type) =>
        type.GetProperties()
            .FirstOrDefault(p => string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Computes the delta between two consecutive collection snapshots.
    /// </summary>
    /// <remarks>
    /// On the first call (when <paramref name="previous"/> is <see langword="null"/>), all current items
    /// are reported as <see cref="ChangeSet.Added"/>.
    /// </remarks>
    /// <param name="previous">The previous snapshot, or <see langword="null"/> for the first update.</param>
    /// <param name="current">The current snapshot.</param>
    /// <returns>A <see cref="ChangeSet"/> describing what changed.</returns>
    public ChangeSet Compute(IEnumerable<object>? previous, IEnumerable<object> current)
    {
        var currentItems = current.ToArray();

        if (previous is null)
        {
            return new ChangeSet { Added = currentItems };
        }

        var previousItems = previous.ToArray();

        if (previousItems.Length == 0 && currentItems.Length == 0)
        {
            return new ChangeSet();
        }

        var itemType = (currentItems.FirstOrDefault() ?? previousItems.FirstOrDefault())?.GetType();
        var idProperty = itemType is not null ? FindIdentityProperty(itemType) : null;

        return idProperty is not null
            ? ComputeWithIdentity(previousItems, currentItems, idProperty)
            : ComputeByJson(previousItems, currentItems);
    }

    /// <summary>
    /// Computes the delta using a discovered identity property.
    /// </summary>
    /// <remarks>
    /// Items with a new identity key are <see cref="ChangeSet.Added"/>; items whose key is gone are
    /// <see cref="ChangeSet.Removed"/>; items with the same key but a different JSON representation are
    /// <see cref="ChangeSet.Replaced"/>.
    /// </remarks>
    /// <param name="previousItems">The previous snapshot items.</param>
    /// <param name="currentItems">The current snapshot items.</param>
    /// <param name="idProperty">The property used to extract the item identity key.</param>
    /// <returns>A <see cref="ChangeSet"/> describing what changed.</returns>
    public ChangeSet ComputeWithIdentity(object[] previousItems, object[] currentItems, PropertyInfo idProperty)
    {
        var previousById = new Dictionary<object, object>(previousItems.Length);
        foreach (var item in previousItems)
        {
            var id = idProperty.GetValue(item);
            if (id is not null)
            {
                previousById[id] = item;
            }
        }

        var currentById = new Dictionary<object, object>(currentItems.Length);
        foreach (var item in currentItems)
        {
            var id = idProperty.GetValue(item);
            if (id is not null)
            {
                currentById[id] = item;
            }
        }

        var added = new List<object>();
        var replaced = new List<object>();
        var removed = new List<object>();

        foreach (var (id, item) in currentById)
        {
            if (!previousById.TryGetValue(id, out var prev))
            {
                added.Add(item);
            }
            else
            {
                var prevJson = JsonSerializer.Serialize(prev, serializerOptions);
                var currJson = JsonSerializer.Serialize(item, serializerOptions);
                if (prevJson != currJson)
                {
                    replaced.Add(item);
                }
            }
        }

        foreach (var (id, item) in previousById)
        {
            if (!currentById.ContainsKey(id))
            {
                removed.Add(item);
            }
        }

        return new ChangeSet { Added = added, Replaced = replaced, Removed = removed };
    }

    /// <summary>
    /// Computes the delta using JSON-hash comparison (fallback when no identity property is found).
    /// </summary>
    /// <remarks>
    /// Only surfaces <see cref="ChangeSet.Added"/> and <see cref="ChangeSet.Removed"/>;
    /// <see cref="ChangeSet.Replaced"/> is never emitted by this method.
    /// </remarks>
    /// <param name="previousItems">The previous snapshot items.</param>
    /// <param name="currentItems">The current snapshot items.</param>
    /// <returns>A <see cref="ChangeSet"/> describing what changed.</returns>
    public ChangeSet ComputeByJson(object[] previousItems, object[] currentItems)
    {
        var previousHashes = previousItems
            .Select(item => JsonSerializer.Serialize(item, serializerOptions))
            .ToHashSet();

        var currentHashes = currentItems
            .Select(item => JsonSerializer.Serialize(item, serializerOptions))
            .ToHashSet();

        var added = currentItems
            .Where(item => !previousHashes.Contains(JsonSerializer.Serialize(item, serializerOptions)))
            .ToArray();

        var removed = previousItems
            .Where(item => !currentHashes.Contains(JsonSerializer.Serialize(item, serializerOptions)))
            .ToArray();

        return new ChangeSet { Added = added, Removed = removed };
    }
}
