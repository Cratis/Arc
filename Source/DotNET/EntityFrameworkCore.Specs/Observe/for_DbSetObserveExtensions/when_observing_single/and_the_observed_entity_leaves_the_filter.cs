// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing_single;

/// <summary>
/// Specs for the ObserveSingle extension method when the observed entity is updated so it no longer
/// matches the filter — the entity still exists, but has left the observed result set.
/// </summary>
public class and_the_observed_entity_leaves_the_filter : a_db_set_observe_context
{
    ISubject<TestEntity> _subject;
    TestEntity _initialEntity;
    TestEntity _afterUpdateEntity;
    bool _hasReceivedAfterUpdate;
    TestEntity _targetEntity;
    ManualResetEventSlim _initialReceived;
    ManualResetEventSlim _updateReceived;

    void Establish()
    {
        _targetEntity = new TestEntity { Name = "Target", IsActive = true };
        SeedTestData(_targetEntity);

        _initialReceived = new ManualResetEventSlim(false);
        _updateReceived = new ManualResetEventSlim(false);
    }

    void Because()
    {
        _subject = _dbContext.TestEntities.ObserveSingle(e => e.Name == "Target");
        _subject.Subscribe(entity =>
        {
            if (!_initialReceived.IsSet)
            {
                _initialEntity = entity;
                _initialReceived.Set();
            }
            else if (!_updateReceived.IsSet)
            {
                _afterUpdateEntity = entity;
                _hasReceivedAfterUpdate = true;
                _updateReceived.Set();
            }
        });

        _initialReceived.Wait(TimeSpan.FromSeconds(10));

        // Renaming the entity moves it out of the "Name == Target" filter without deleting it.
        UpdateDirectlyIntoDatabase(_targetEntity.Id, "Renamed");

        _updateReceived.Wait(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_receive_the_entity_initially() => _initialEntity.ShouldNotBeNull();
    [Fact] void should_receive_an_emission_after_leaving_the_filter() => _hasReceivedAfterUpdate.ShouldBeTrue();
    [Fact] void should_emit_the_default_value_after_leaving_the_filter() => _afterUpdateEntity.ShouldBeNull();
}
