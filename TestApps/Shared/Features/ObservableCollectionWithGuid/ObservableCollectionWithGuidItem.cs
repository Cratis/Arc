// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;

namespace TestApps.Features.ObservableCollectionWithGuid;

/// <summary>
/// Represents an item in the Guid-based observable collection test feature.
/// </summary>
/// <param name="Id">The item identifier.</param>
/// <param name="Label">The item label.</param>
[ReadModel]
[AllowAnonymous]
public record ObservableCollectionWithGuidItem(Guid Id, string Label)
{
    static readonly BehaviorSubject<IEnumerable<ObservableCollectionWithGuidItem>> _all = new(
    [
        new ObservableCollectionWithGuidItem(new Guid(0x11111111, 0x1111, 0x1111, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11), "One"),
        new ObservableCollectionWithGuidItem(new Guid(0x22222222, 0x2222, 0x2222, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22, 0x22), "Two"),
    ]);

    /// <summary>
    /// Observes the current collection and pushes updates when items are added or removed.
    /// </summary>
    /// <returns>An observable sequence of the current collection.</returns>
    public static ISubject<IEnumerable<ObservableCollectionWithGuidItem>> All()
    {
        var relay = new BehaviorSubject<IEnumerable<ObservableCollectionWithGuidItem>>(_all.Value);
        _all.Subscribe(relay);
        return relay;
    }

    internal static void Add(Guid id, string label) =>
        _all.OnNext(_all.Value.Append(new ObservableCollectionWithGuidItem(id, label)));

    internal static void Remove(Guid id) =>
        _all.OnNext(_all.Value.Where(item => item.Id != id));
}