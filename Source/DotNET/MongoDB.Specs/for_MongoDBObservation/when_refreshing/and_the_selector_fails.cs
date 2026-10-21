// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBObservation.when_refreshing;

public class and_the_selector_fails : given.an_observation
{
    Exception _failure;

    void Establish() => _failure = new Exception("selector failed");

    async Task Because()
    {
        var ready = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        _snapshot = ready.Task.WaitAsync;
        _subject = MongoDBObservation.Create<int, int>(
            _source,
            new HashSet<CollectionNamespace> { new("testdb", "items") },
            _snapshot,
            _ => throw _failure,
            NullLogger<MongoDBWatcher>.Instance,
            _delay);
        _subject.Subscribe(_ => { }, error => _error = error);
        ready.SetResult(1);
        await _detached.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_preserve_the_selector_failure() => _error.ShouldBeSame(_failure);
    [Fact] void should_not_retry_the_selector() => _delays.ShouldBeEmpty();
    [Fact] void should_end_the_subscription() => _changes.HasObservers.ShouldBeFalse();
}
