// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Extends the collection abstractions with what collecting analysis results needs.
/// </summary>
public static class Collections
{
    /// <summary>
    /// Adds everything from a sequence to a collection.
    /// </summary>
    /// <typeparam name="T">The type of the items.</typeparam>
    /// <param name="target">The collection to add to.</param>
    /// <param name="items">The items to add.</param>
    public static void AddRange<T>(this ICollection<T> target, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            target.Add(item);
        }
    }
}
