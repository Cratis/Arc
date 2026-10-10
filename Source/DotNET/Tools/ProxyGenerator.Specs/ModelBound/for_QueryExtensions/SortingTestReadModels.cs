// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.TestTypes.Sorting;

[ReadModel]
public record Listing(string Name, decimal Price)
{
    public static IQueryable<Listing> All(string id) => new[] { new Listing(id, 10m) }.AsQueryable();

    public static Listing[] AllAsArray(string id) => [new Listing(id, 10m)];

    public static ISubject<IEnumerable<Listing>> ObserveAll(string id) => new BehaviorSubject<IEnumerable<Listing>>([new Listing(id, 10m)]);
}
