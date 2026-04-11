// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;

namespace TestApps.Features.ChangeStream;

/// <summary>
/// Represents an item in the change stream showcase collection.
/// </summary>
/// <param name="Id">The unique identifier of the item.</param>
/// <param name="Label">A descriptive label for the item.</param>
/// <param name="Value">A numeric value associated with the item.</param>
[ReadModel]
[AllowAnonymous]
public record ChangeStreamItem(int Id, string Label, int Value)
{
    static readonly BehaviorSubject<IEnumerable<ChangeStreamItem>> _all = new(
    [
        new ChangeStreamItem(1, "Alpha", 10),
        new ChangeStreamItem(2, "Beta", 20),
        new ChangeStreamItem(3, "Gamma", 30),
    ]);

    /// <summary>
    /// Observes the full collection, pushing every change to subscribers.
    /// </summary>
    /// <returns>An observable emitting the current collection on each mutation.</returns>
    public static ISubject<IEnumerable<ChangeStreamItem>> All() => _all;

    internal static void Add(int id, string label, int value) =>
        _all.OnNext(_all.Value.Append(new ChangeStreamItem(id, label, value)));

    internal static void Update(int id, string label, int value) =>
        _all.OnNext(_all.Value.Select(item => item.Id == id ? new ChangeStreamItem(id, label, value) : item));

    internal static void Remove(int id) =>
        _all.OnNext(_all.Value.Where(item => item.Id != id));
}
