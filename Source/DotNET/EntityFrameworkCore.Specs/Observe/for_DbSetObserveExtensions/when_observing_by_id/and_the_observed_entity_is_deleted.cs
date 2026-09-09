// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing_by_id;

/// <summary>
/// Specs for the ObserveById extension method when the observed entity is deleted.
/// </summary>
public class and_the_observed_entity_is_deleted : a_db_set_observe_context
{
    ISubject<TestEntity> _subject;
    TestEntity _initialEntity;
    TestEntity _afterDeleteEntity;
    bool _hasReceivedAfterDelete;
    TestEntity _targetEntity;
    ManualResetEventSlim _initialReceived;
    ManualResetEventSlim _deleteReceived;

    void Establish()
    {
        _targetEntity = new TestEntity { Name = "Target", IsActive = true };
        SeedTestData(_targetEntity);

        _initialReceived = new ManualResetEventSlim(false);
        _deleteReceived = new ManualResetEventSlim(false);
    }

    void Because()
    {
        _subject = _dbContext.TestEntities.ObserveById(_targetEntity.Id);
        _subject.Subscribe(entity =>
        {
            if (!_initialReceived.IsSet)
            {
                _initialEntity = entity;
                _initialReceived.Set();
            }
            else if (!_deleteReceived.IsSet)
            {
                _afterDeleteEntity = entity;
                _hasReceivedAfterDelete = true;
                _deleteReceived.Set();
            }
        });

        _initialReceived.Wait(TimeSpan.FromSeconds(10));

        DeleteDirectlyFromDatabase(_targetEntity.Id);

        _deleteReceived.Wait(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_receive_the_entity_initially() => _initialEntity.ShouldNotBeNull();
    [Fact] void should_receive_an_emission_after_the_delete() => _hasReceivedAfterDelete.ShouldBeTrue();
    [Fact] void should_emit_the_default_value_after_the_delete() => _afterDeleteEntity.ShouldBeNull();
}
