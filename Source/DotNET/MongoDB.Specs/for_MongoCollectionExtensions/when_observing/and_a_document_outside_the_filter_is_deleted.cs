// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing;

public class and_a_document_outside_the_filter_is_deleted : given.a_server_backed_observation
{
    ObservedDocument _excluded;
    ObservedDocument _inserted;
    Emission _initial;
    Emission _afterChanges;

    void Establish()
    {
        _excluded = new(Guid.NewGuid(), Excluded);
        _inserted = new(Guid.NewGuid(), "C");
        _documents = [new(Guid.NewGuid(), "A"), new(Guid.NewGuid(), "B"), _excluded];
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

    [Fact] void should_start_with_the_matching_total() => _initial.TotalItems.ShouldEqual(2);
    [Fact] void should_not_lose_a_matching_document_from_the_total() => _afterChanges.TotalItems.ShouldEqual(3);
    [Fact] void should_report_only_the_insert() => Assert.Equal([new(CollectionChangeKind.Added, _inserted.Id)], _afterChanges.Changes);
}
