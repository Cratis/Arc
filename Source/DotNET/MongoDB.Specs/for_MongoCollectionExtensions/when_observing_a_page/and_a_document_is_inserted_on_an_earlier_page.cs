// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing_a_page;

public class and_a_document_is_inserted_on_an_earlier_page : given.a_server_backed_observation
{
    ObservedDocument _pushedOnto;
    ObservedDocument _pushedOff;
    ObservedDocument _inserted;
    Emission _initial;
    Emission _afterChange;

    void Establish()
    {
        PageByName(1, 2);
        _pushedOnto = new(Guid.NewGuid(), "B");
        _pushedOff = new(Guid.NewGuid(), "D");
        _inserted = new(Guid.NewGuid(), "Aa");
        _documents = [new(Guid.NewGuid(), "A"), _pushedOnto, new(Guid.NewGuid(), "C"), _pushedOff];
    }

    async Task Because()
    {
        _initial = await StartObserving();
        _documents.Add(_inserted);
        PushChange(InsertOf(_inserted));
        _afterChange = await NextEmission();
    }

    [Fact] void should_start_with_the_second_page() => Assert.Equal(["C", "D"], _initial.Page.Select(_ => _.Name));
    [Fact] void should_shift_the_page_by_one() => Assert.Equal(["B", "C"], _afterChange.Page.Select(_ => _.Name));
    [Fact] void should_count_the_inserted_document() => _afterChange.TotalItems.ShouldEqual(5);
    [Fact] void should_report_what_left_and_entered_the_page() => Assert.Equal([new(CollectionChangeKind.Removed, _pushedOff.Id), new(CollectionChangeKind.Added, _pushedOnto.Id)], _afterChange.Changes);
}
