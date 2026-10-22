// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing;

public class and_the_shared_cursor_is_not_open : given.an_observation_with_an_opening_watch
{
    bool _emittedBeforeOpen;
    bool _countedBeforeOpen;
    bool _queriedBeforeOpen;
    ObservedDocument _inserted;
    Emission _initial;

    async Task Because()
    {
        var initial = StartObserving();
        await _watchRequested.Task.WaitAsync(Deadline);
        // The cursor is deliberately slow: keep it unopened while checking for a premature query.
        await Task.WhenAny(initial, Task.Delay(200));
        _emittedBeforeOpen = initial.IsCompleted;
        _countedBeforeOpen = _collection.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "CountDocumentsAsync");
        _queriedBeforeOpen = _collection.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "FindAsync");

        // This write precedes cursor creation, so MongoDB will not deliver it on that cursor.
        _inserted = new(Guid.NewGuid(), "Inserted before open");
        _documents.Add(_inserted);
        _openCursor.SetResult(_cursor);
        _initial = await initial.WaitAsync(Deadline);
    }

    [Fact] void should_not_emit_before_the_cursor_opens() => _emittedBeforeOpen.ShouldBeFalse();
    [Fact] void should_not_count_before_the_cursor_opens() => _countedBeforeOpen.ShouldBeFalse();
    [Fact] void should_not_query_before_the_cursor_opens() => _queriedBeforeOpen.ShouldBeFalse();
    [Fact] void should_include_the_insert_in_the_initial_snapshot() => _initial.Page.ShouldContain(_inserted);
    [Fact] void should_count_the_insert_in_the_initial_snapshot() => _initial.TotalItems.ShouldEqual(_documents.Count);
}
