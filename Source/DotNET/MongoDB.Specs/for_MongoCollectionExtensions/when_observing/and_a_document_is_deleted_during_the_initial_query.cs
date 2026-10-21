// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing;

public class and_a_document_is_deleted_during_the_initial_query : given.a_server_backed_observation
{
    ObservedDocument _deleted;
    Emission _initial;
    Emission _afterDelete;

    void Establish() => _deleted = _documents[0];

    async Task Because()
    {
        // The observation is attached to the shared stream before it reads, so a change arriving while the initial
        // query is still running is held and applied after it rather than lost.
        var initial = StartObserving();
        await WaitForSubscribers(1);
        PushChange(DeleteOf(_deleted.Id));
        _initial = await initial;
        _documents.Remove(_deleted);
        _afterDelete = await NextEmission();
    }

    [Fact] void should_read_the_snapshot_the_query_returned() => _initial.Page.ShouldContain(_deleted);
    [Fact] void should_apply_the_buffered_delete() => Assert.Equal([new(CollectionChangeKind.Removed, _deleted.Id)], _afterDelete.Changes);
    [Fact] void should_no_longer_contain_the_document() => _afterDelete.Page.ShouldNotContain(_deleted);
}
