// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing;

public class and_a_change_happens_in_another_collection : given.a_server_backed_observation
{
    ObservedDocument _elsewhere;
    ObservedDocument _inserted;
    Emission _afterChanges;

    void Establish()
    {
        _elsewhere = new(Guid.NewGuid(), "Elsewhere");
        _inserted = new(Guid.NewGuid(), "C");
    }

    async Task Because()
    {
        await StartObserving();
        PushChange(ChangeIn("another_collection", "insert", _elsewhere));
        PushChange(ChangeIn("another_collection", "delete", _elsewhere));
        _documents.Add(_inserted);
        PushChange(InsertOf(_inserted));
        _afterChanges = await NextEmission();
    }

    [Fact] void should_report_only_the_change_in_the_observed_collection() => Assert.Equal([new(CollectionChangeKind.Added, _inserted.Id)], _afterChanges.Changes);
    [Fact] void should_count_only_the_observed_collection() => _afterChanges.TotalItems.ShouldEqual(3);
}
