// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.MongoDB.for_MongoDBDatabaseWatch.given;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBObservation.when_starting;

public class and_the_shared_cursor_is_not_open : a_database_watch
{
    ISubject<int> _subject;
    bool _queriedBeforeOpen;

    async Task Because()
    {
        var queried = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var emitted = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        _subject = MongoDBObservation.Create(
            _watch,
            new HashSet<CollectionNamespace> { new("testdb", "items") },
            _ =>
            {
                queried.TrySetResult(1);
                return Task.FromResult(1);
            },
            value => value,
            NullLogger<MongoDBWatcher>.Instance);
        using var subscription = _subject.Subscribe(value => emitted.TrySetResult(value));

        // Keep the cursor deliberately unopened during this assertion window.
        await Task.WhenAny(queried.Task, Task.Delay(200));
        _queriedBeforeOpen = queried.Task.IsCompleted;
        _worker = _watch.Run("testdb");
        await emitted.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    void Destroy() => (_subject as IDisposable)?.Dispose();

    [Fact] void should_wait_for_the_shared_cursor_before_querying() => _queriedBeforeOpen.ShouldBeFalse();
}
