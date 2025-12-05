// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing_with_filter;

public class and_filter_matches_some_entities : a_db_set_observe_context
{
    ISubject<IEnumerable<TestEntity>> _subject;
    IEnumerable<TestEntity> _receivedEntities;
    ManualResetEventSlim _receivedEvent;

    void Establish()
    {
        SeedTestData(
            new TestEntity { Id = 1, Name = "Active1", IsActive = true },
            new TestEntity { Id = 2, Name = "Inactive1", IsActive = false },
            new TestEntity { Id = 3, Name = "Active2", IsActive = true },
            new TestEntity { Id = 4, Name = "Inactive2", IsActive = false });

        _receivedEvent = new ManualResetEventSlim(false);
        _receivedEntities = [];
    }

    void Because()
    {
        _subject = _dbContext.TestEntities.Observe(e => e.IsActive);
        _subject.Subscribe(entities =>
        {
            var list = entities.ToList();

            // Only set if we have actual data (skip the initial empty from BehaviorSubject)
            if (list.Count > 0 || _receivedEvent.IsSet)
            {
                _receivedEntities = list;
                _receivedEvent.Set();
            }
        });

        // Wait for the initial query to complete (with data)
        _receivedEvent.Wait(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_only_return_matching_entities() => _receivedEntities.Count().ShouldEqual(2);
    [Fact] void should_return_first_active_entity() => _receivedEntities.ShouldContain(e => e.Name == "Active1");
    [Fact] void should_return_second_active_entity() => _receivedEntities.ShouldContain(e => e.Name == "Active2");
    [Fact] void should_not_return_inactive_entities() => _receivedEntities.ShouldNotContain(e => !e.IsActive);

    void Cleanup()
    {
        _subject?.OnCompleted();
        _receivedEvent?.Dispose();
    }
}
