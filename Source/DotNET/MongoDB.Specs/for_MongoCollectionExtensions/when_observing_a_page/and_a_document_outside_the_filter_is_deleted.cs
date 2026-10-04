// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing_a_page;

public class and_a_document_outside_the_filter_is_deleted : given.a_server_backed_observation
{
    ObservedDocument _excluded;
    ObservedDocument _inserted;
    Emission _initial;
    Emission _afterChanges;

    void Establish()
    {
        PageByName(0, 2);
        _excluded = new(Guid.NewGuid(), Excluded);
        _inserted = new(Guid.NewGuid(), "Aa");
        _documents = [new(Guid.NewGuid(), "A"), new(Guid.NewGuid(), "B"), new(Guid.NewGuid(), "C"), _excluded];
    }

    async Task Because()
    {
        _initial = await StartObserving();
        _documents.Remove(_excluded);
        PushChange(DeleteOf(_excluded.Id));
        _documents.Add(_inserted);
        PushChange(InsertOf(_inserted));
        _afterChanges = await NextEmission();
    }

    [Fact] void should_start_with_the_matching_total() => _initial.TotalItems.ShouldEqual(3);
    [Fact] void should_not_lose_a_matching_document_from_the_total() => _afterChanges.TotalItems.ShouldEqual(4);
    [Fact] void should_show_the_inserted_document_on_the_page() => Assert.Equal(["A", "Aa"], _afterChanges.Page.Select(_ => _.Name));
}
