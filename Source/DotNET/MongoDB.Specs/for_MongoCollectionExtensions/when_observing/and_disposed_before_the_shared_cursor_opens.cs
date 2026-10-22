// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing;

public class and_disposed_before_the_shared_cursor_opens : given.an_observation_with_an_opening_watch
{
    async Task Because()
    {
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _subject = _collection.Observe();
        using var subscription = _subject.Subscribe(_ => { }, _ => { }, () => completed.TrySetResult());
        await _watchRequested.Task.WaitAsync(Deadline);
        ((IDisposable)_subject).Dispose();
        await completed.Task.WaitAsync(Deadline);
    }

    [Fact] void should_not_count_documents() => _collection.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "CountDocumentsAsync").ShouldBeFalse();
    [Fact] void should_not_query_documents() => _collection.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "FindAsync").ShouldBeFalse();
}
