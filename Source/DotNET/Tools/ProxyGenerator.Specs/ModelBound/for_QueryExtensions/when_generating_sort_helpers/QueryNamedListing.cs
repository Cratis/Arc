// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.TestTypes.Sorting;

[ReadModel]
public record QueryNamedListing(string Query, decimal Price)
{
    public static QueryNamedListing[] AllAsArray() => [new QueryNamedListing("q", 10m)];

    public static ISubject<IEnumerable<QueryNamedListing>> ObserveAll() => new BehaviorSubject<IEnumerable<QueryNamedListing>>([new QueryNamedListing("q", 10m)]);
}
