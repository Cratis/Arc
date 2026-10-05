// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing_a_page;

public class and_a_document_changes_its_sort_value : given.a_server_backed_observation
{
    ObservedDocument _first;
    ObservedDocument _second;
    ObservedDocument _third;
    ObservedDocument _moved;
    Emission _afterChange;

    void Establish()
    {
        PageByName(0, 3);
        _first = new(Guid.NewGuid(), "A");
        _second = new(Guid.NewGuid(), "B");
        _third = new(Guid.NewGuid(), "C");
        _moved = _first with { Name = "Bz" };
        _documents = [_first, _second, _third];
        StubUpdateBelongsCheck(belongs: true);
    }

    async Task Because()
    {
        await StartObserving();
        _documents[0] = _moved;
        PushChange(UpdateOf(_moved));
        _afterChange = await NextEmission();
    }

    [Fact] void should_place_the_document_by_its_new_sort_value() => Assert.Equal([_second, _moved, _third], _afterChange.Page);
    [Fact] void should_report_the_document_as_replaced() => Assert.Equal([new(CollectionChangeKind.Replaced, _first.Id)], _afterChange.Changes);
    [Fact] void should_keep_the_total() => _afterChange.TotalItems.ShouldEqual(3);
}
