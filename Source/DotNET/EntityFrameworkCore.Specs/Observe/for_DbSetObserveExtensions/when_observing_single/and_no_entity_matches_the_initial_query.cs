// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing_single;

/// <summary>
/// Specs for the ObserveSingle extension method when the initial query finds no matching entity.
/// </summary>
public class and_no_entity_matches_the_initial_query : a_db_set_observe_context
{
    ISubject<TestEntity> _subject;
    TestEntity _receivedEntity;
    bool _hasReceived;
    ManualResetEventSlim _receivedEvent;

    void Establish()
    {
        SeedTestData(new TestEntity { Name = "OtherEntity", IsActive = false });
        _receivedEvent = new ManualResetEventSlim(false);
    }

    void Because()
    {
        _subject = _dbContext.TestEntities.ObserveSingle(e => e.IsActive);
        _subject.Subscribe(entity =>
        {
            _receivedEntity = entity;
            _hasReceived = true;
            _receivedEvent.Set();
        });

        _receivedEvent.Wait(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_emit() => _hasReceived.ShouldBeTrue();
    [Fact] void should_emit_the_default_value() => _receivedEntity.ShouldBeNull();
}
