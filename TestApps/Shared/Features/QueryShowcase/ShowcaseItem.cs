// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;

namespace TestApps.Features.QueryShowcase;

/// <summary>
/// Represents a showcase item for demonstrating different query types.
/// </summary>
/// <param name="Id">The item identifier.</param>
/// <param name="Name">The item name.</param>
/// <param name="UpdatedAt">When the item was last updated.</param>
[ReadModel]
[AllowAnonymous]
public record ShowcaseItem(int Id, string Name, DateTimeOffset UpdatedAt)
{
    static readonly BehaviorSubject<ShowcaseItem> _latest = new(new ShowcaseItem(1, "Initial", DateTimeOffset.UtcNow));

    static readonly BehaviorSubject<IEnumerable<ShowcaseItem>> _all = new(
    [
        new ShowcaseItem(1, "Alpha", DateTimeOffset.UtcNow),
        new ShowcaseItem(2, "Beta", DateTimeOffset.UtcNow),
        new ShowcaseItem(3, "Gamma", DateTimeOffset.UtcNow),
    ]);

    static ShowcaseItem()
    {
        _ = Task.Run(async () =>
        {
            var tick = 0;

            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
                tick++;
                _latest.OnNext(new ShowcaseItem(tick, $"Update #{tick}", DateTimeOffset.UtcNow));
                _all.OnNext(
                [
                    new ShowcaseItem(1, $"Alpha (v{tick})", DateTimeOffset.UtcNow),
                    new ShowcaseItem(2, $"Beta (v{tick})", DateTimeOffset.UtcNow),
                    new ShowcaseItem(3, $"Gamma (v{tick})", DateTimeOffset.UtcNow),
                    new ShowcaseItem(4, $"Delta (v{tick})", DateTimeOffset.UtcNow),
                ]);
            }
        });
    }

    /// <summary>
    /// Query type 1: Observable, returns a single item — streams the latest showcase item.
    /// </summary>
    /// <returns>An observable emitting the latest item every 3 seconds.</returns>
    public static ISubject<ShowcaseItem> Latest() => _latest;

    /// <summary>
    /// Query type 2: Observable, returns a collection — streams the full list.
    /// Shared by multiple subscribers through the QueryInstanceCache.
    /// </summary>
    /// <returns>An observable emitting the current list on each update.</returns>
    public static ISubject<IEnumerable<ShowcaseItem>> All()
    {
        var relay = new BehaviorSubject<IEnumerable<ShowcaseItem>>(_all.Value);
        _all.Subscribe(relay);
        return relay;
    }

    /// <summary>
    /// Query type 3: Regular (non-observable), returns a single item by identifier.
    /// </summary>
    /// <param name="id">The item identifier.</param>
    /// <returns>The matching item.</returns>
    public static ShowcaseItem ById(int id) =>
        new(id, $"Item #{id}", DateTimeOffset.UtcNow);

    /// <summary>
    /// Query type 4: Regular (non-observable), returns a fixed collection as <see cref="IQueryable{T}"/>.
    /// </summary>
    /// <returns>A queryable sequence of static showcase items.</returns>
    public static IQueryable<ShowcaseItem> GetAll() =>
        Enumerable.Range(1, 5)
            .Select(i => new ShowcaseItem(i, $"Static Item #{i}", DateTimeOffset.UtcNow))
            .AsQueryable();
}
