// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;

namespace TestApps.Features.ObservableCollection;

/// <summary>
/// Represents an item in the observable collection test feature.
/// </summary>
/// <param name="Id">The item identifier.</param>
/// <param name="Label">The item label.</param>
[ReadModel]
[AllowAnonymous]
public record ObservableCollectionItem(int Id, string Label)
{
    static readonly BehaviorSubject<IEnumerable<ObservableCollectionItem>> _all = new(
    [
        new ObservableCollectionItem(1, "One"),
        new ObservableCollectionItem(2, "Two"),
    ]);

    /// <summary>
    /// Observes the current collection and pushes updates when items are added or removed.
    /// </summary>
    /// <returns>An observable sequence of the current collection.</returns>
    public static ISubject<IEnumerable<ObservableCollectionItem>> All()
    {
        var relay = new BehaviorSubject<IEnumerable<ObservableCollectionItem>>(_all.Value);
        _all.Subscribe(relay);
        return relay;
    }

    internal static void Add(int id, string label) =>
        _all.OnNext(_all.Value.Append(new ObservableCollectionItem(id, label)));

    internal static void Remove(int id) =>
        _all.OnNext(_all.Value.Where(item => item.Id != id));
}