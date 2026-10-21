// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_changes_arrive_while_one_is_applied;

public class and_more_documents_change_than_are_held : given.a_busy_observation
{
    ObservedDocument _first;
    List<ObservedDocument> _inserted;
    Emission _afterChanges;
    int _checksAfterChanges;
    int _readsAfterChanges;

    void Establish()
    {
        _first = new(Guid.NewGuid(), "First inserted");
        _inserted = [.. Enumerable.Range(0, PendingChanges.MaximumDocuments + 1).Select(index => new ObservedDocument(Guid.NewGuid(), $"Inserted {index}"))];
    }

    async Task Because()
    {
        await StartObserving();
        _documents.Add(_first);
        PushChange(InsertOf(_first));
        await Busy();
        foreach (var document in _inserted)
        {
            _documents.Add(document);
            PushChange(InsertOf(document));
        }

        Release();
        await NextEmission();
        _afterChanges = await NextEmission();
        await Task.Delay(200);
        _checksAfterChanges = Volatile.Read(ref _belongsChecks);
        _readsAfterChanges = Volatile.Read(ref _reads);
    }

    [Fact] void should_hold_every_document() => _afterChanges.Page.Count.ShouldEqual(_documents.Count);
    [Fact] void should_check_only_the_held_change() => _checksAfterChanges.ShouldEqual(1);
    [Fact] void should_read_once_more_instead_of_once_per_document() => _readsAfterChanges.ShouldEqual(2);
}
