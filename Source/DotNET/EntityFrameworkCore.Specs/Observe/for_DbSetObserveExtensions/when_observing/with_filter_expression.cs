// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing;

/// <summary>
/// Specs for the Observe extension method with a filter expression.
/// </summary>
public class with_filter_expression : a_db_set_observe_context
{
    ISubject<IEnumerable<TestEntity>> _subject;
    IEnumerable<TestEntity> _receivedEntities;
    ManualResetEventSlim _receivedEvent;

    void Establish()
    {
        SeedTestData(
            new TestEntity { Name = "Active1", IsActive = true },
            new TestEntity { Name = "Inactive1", IsActive = false },
            new TestEntity { Name = "Active2", IsActive = true },
            new TestEntity { Name = "Inactive2", IsActive = false });

        _receivedEvent = new ManualResetEventSlim(false);
        _receivedEntities = [];
    }

    void Because()
    {
        _subject = _dbContext.TestEntities.Observe(e => e.IsActive);
        _subject.Subscribe(entities =>
        {
            var list = entities.ToList();
            if (list.Count > 0 || _receivedEvent.IsSet)
            {
                _receivedEntities = list;
                _receivedEvent.Set();
            }
        });

        _receivedEvent.Wait(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_only_return_matching_entities() => _receivedEntities.Count().ShouldEqual(2);
    [Fact] void should_return_first_active_entity() => _receivedEntities.ShouldContain(e => e.Name == "Active1");
    [Fact] void should_return_second_active_entity() => _receivedEntities.ShouldContain(e => e.Name == "Active2");
    [Fact] void should_not_return_inactive_entities() => _receivedEntities.ShouldNotContain(e => !e.IsActive);
}
