// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.EntityFrameworkCore.PostgreSql.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.PostgreSql.Observe.for_DbSetObserveExtensions.when_observing_single;

/// <summary>
/// Specs for the ObserveSingle extension method detecting external updates.
/// </summary>
/// <param name="fixture">The PostgreSQL fixture.</param>
[Collection(PostgreSqlCollection.Name)]
public class and_external_update_occurs(PostgreSqlFixture fixture) : a_postgresql_observe_context(fixture)
{
    ISubject<TestEntity> _subject;
    List<TestEntity> _receivedUpdates;
    ManualResetEventSlim _initialReceived;
    ManualResetEventSlim _updateReceived;
    TestEntity _seededEntity;

    void Establish()
    {
        _seededEntity = new TestEntity { Name = "OriginalName", IsActive = true };
        SeedTestData(_seededEntity);

        _receivedUpdates = [];
        _initialReceived = new ManualResetEventSlim(false);
        _updateReceived = new ManualResetEventSlim(false);
    }

    void Because()
    {
        var entityId = _seededEntity.Id;
        _subject = _dbContext.TestEntities.ObserveSingle(e => e.Id == entityId);
        _subject.Subscribe(entity =>
        {
            _receivedUpdates.Add(entity);

            if (!_initialReceived.IsSet)
            {
                _initialReceived.Set();
            }
            else if (entity.Name == "UpdatedName")
            {
                _updateReceived.Set();
            }
        });

        // Wait for initial data
        _initialReceived.Wait(TimeSpan.FromSeconds(10));

        // Simulate external process updating data
        LogInfo("Updating entity directly in database...");
        UpdateDirectlyInDatabase(entityId, "UpdatedName");

        // Wait for notification
        _updateReceived.Wait(TimeSpan.FromSeconds(30));
    }

    [Fact] void should_receive_initial_data() => _receivedUpdates.Count.ShouldBeGreaterThanOrEqual(1);
    [Fact] void should_detect_updated_entity() => _receivedUpdates[^1].Name.ShouldEqual("UpdatedName");
}
