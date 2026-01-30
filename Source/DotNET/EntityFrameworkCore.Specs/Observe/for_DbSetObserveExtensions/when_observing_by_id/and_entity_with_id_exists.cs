// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing_by_id;

/// <summary>
/// Specs for the ObserveById extension method returning initial data.
/// </summary>
public class and_entity_with_id_exists : a_db_set_observe_context
{
    ISubject<TestEntity> _subject;
    TestEntity _receivedEntity;
    ManualResetEventSlim _receivedEvent;
    TestEntity _targetEntity;

    void Establish()
    {
        _targetEntity = new TestEntity { Name = "Entity2", IsActive = false };
        SeedTestData(
            new TestEntity { Name = "Entity1", IsActive = true },
            _targetEntity,
            new TestEntity { Name = "Entity3", IsActive = true });

        _receivedEvent = new ManualResetEventSlim(false);
    }

    void Because()
    {
        _subject = _dbContext.TestEntities.ObserveById(_targetEntity.Id);
        _subject.Subscribe(entity =>
        {
            _receivedEntity = entity;
            _receivedEvent.Set();
        });

        _receivedEvent.Wait(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_return_entity_with_matching_id() => _receivedEntity.ShouldNotBeNull();
    [Fact] void should_return_correct_entity() => _receivedEntity.Name.ShouldEqual("Entity2");
    [Fact] void should_return_entity_with_correct_id() => _receivedEntity.Id.ShouldEqual(_targetEntity.Id);
}
