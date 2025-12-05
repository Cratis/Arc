// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing_with_filter;

public class and_filter_matches_no_entities : a_db_set_observe_context
{
    ISubject<IEnumerable<TestEntity>> _subject;
    IEnumerable<TestEntity> _receivedEntities;
    ManualResetEventSlim _receivedEvent;

    void Establish()
    {
        SeedTestData(
            new TestEntity { Id = 1, Name = "Inactive1", IsActive = false },
            new TestEntity { Id = 2, Name = "Inactive2", IsActive = false });

        _receivedEvent = new ManualResetEventSlim(false);
        _receivedEntities = [];
    }

    void Because()
    {
        _subject = _dbContext.TestEntities.Observe(e => e.IsActive);

        // Subscribe and wait for a short delay to allow the async operation to complete
        _subject.Subscribe(entities =>
        {
            _receivedEntities = entities.ToList();
            _receivedEvent.Set();
        });

        // Wait for the initial query to complete - but this may be empty
        // The BehaviorSubject always emits, so we wait a bit longer
        _receivedEvent.Wait(TimeSpan.FromSeconds(5));

        // Give a small delay for the async Task to fully execute
        Thread.Sleep(200);
    }

    [Fact] void should_return_empty_collection() => _receivedEntities.ShouldBeEmpty();

    void Cleanup()
    {
        _subject?.OnCompleted();
        _receivedEvent?.Dispose();
    }
}
