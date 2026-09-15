// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing_by_id;

/// <summary>
/// Specs for the ObserveById extension method when a different entity is deleted. The change-tracking
/// callback is registered per table rather than per id, so any write to the table re-queries and
/// re-emits — this proves that re-emission still reports the observed entity rather than spuriously
/// flipping to the "gone" default value.
/// </summary>
public class and_a_different_entity_is_deleted : a_db_set_observe_context
{
    ISubject<TestEntity> _subject;
    TestEntity _initialEntity;
    TestEntity _afterUnrelatedDeleteEntity;
    bool _hasReceivedSecondEmission;
    TestEntity _targetEntity;
    TestEntity _otherEntity;
    ManualResetEventSlim _initialReceived;
    ManualResetEventSlim _secondReceived;

    void Establish()
    {
        _targetEntity = new TestEntity { Name = "Target", IsActive = true };
        _otherEntity = new TestEntity { Name = "Other", IsActive = true };
        SeedTestData(_targetEntity, _otherEntity);

        _initialReceived = new ManualResetEventSlim(false);
        _secondReceived = new ManualResetEventSlim(false);
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
            else if (!_secondReceived.IsSet)
            {
                _afterUnrelatedDeleteEntity = entity;
                _hasReceivedSecondEmission = true;
                _secondReceived.Set();
            }
        });

        _initialReceived.Wait(TimeSpan.FromSeconds(10));

        DeleteDirectlyFromDatabase(_otherEntity.Id);

        _secondReceived.Wait(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_receive_the_entity_initially() => _initialEntity.ShouldNotBeNull();
    [Fact] void should_receive_a_re_emission() => _hasReceivedSecondEmission.ShouldBeTrue();
    [Fact] void should_still_emit_the_observed_entity() => _afterUnrelatedDeleteEntity.ShouldNotBeNull();
    [Fact] void should_not_have_flipped_to_the_default_value() => _afterUnrelatedDeleteEntity.Id.ShouldEqual(_targetEntity.Id);
}
