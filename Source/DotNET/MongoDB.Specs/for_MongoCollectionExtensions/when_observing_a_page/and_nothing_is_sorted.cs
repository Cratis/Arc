// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing_a_page;

public class and_nothing_is_sorted : given.a_server_backed_observation
{
    string[] _sortFields;

    void Establish()
    {
        PageByName(0, 2);
        _queryContext = _queryContext with { Sorting = Sorting.None };
    }

    async Task Because()
    {
        await StartObserving();
        _sortFields = [.. RenderedSort()?.Names ?? []];
    }

    [Fact] void should_sort_by_the_id() => Assert.Equal(["_id"], _sortFields);
}
