// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing;

public class and_an_inserted_document_is_outside_the_filter : given.a_server_backed_observation
{
    ObservedDocument _excluded;
    ObservedDocument _inserted;
    Emission _afterChanges;

    void Establish()
    {
        _excluded = new(Guid.NewGuid(), Excluded);
        _inserted = new(Guid.NewGuid(), "C");
    }

    async Task Because()
    {
        await StartObserving();

        // The shared stream is not narrowed by the observed filter, so the observation has to ask whether the
        // inserted document belongs, and an answer of no must leave the result and its total as they were.
        StubBelongsCheck(false);
        PushChange(InsertOf(_excluded));
        await WaitForBelongsChecks(1);
        StubBelongsCheck(true);
        _documents.Add(_inserted);
        PushChange(InsertOf(_inserted));
        _afterChanges = await NextEmission();
    }

    [Fact] void should_report_only_the_document_inside_the_filter() => Assert.Equal([new(CollectionChangeKind.Added, _inserted.Id)], _afterChanges.Changes);
    [Fact] void should_not_count_the_document_outside_the_filter() => _afterChanges.TotalItems.ShouldEqual(3);
}
