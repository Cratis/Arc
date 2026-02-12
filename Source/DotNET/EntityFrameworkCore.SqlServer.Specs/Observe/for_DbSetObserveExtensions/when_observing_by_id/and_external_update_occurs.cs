// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.EntityFrameworkCore.SqlServer.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.SqlServer.Observe.for_DbSetObserveExtensions.when_observing_by_id;

/// <summary>
/// Specs for the ObserveById extension method detecting external updates to the observed entity.
/// </summary>
/// <param name="fixture">The SQL Server fixture.</param>
[Collection(SqlServerCollection.Name)]
public class and_external_update_occurs(SqlServerFixture fixture) : a_sql_server_observe_context(fixture)
{
    ISubject<TestEntity> _subject;
    List<TestEntity> _receivedUpdates;
    ManualResetEventSlim _initialReceived;
    ManualResetEventSlim _updateReceived;
    TestEntity _targetEntity;

    void Establish()
    {
        _targetEntity = new TestEntity { Name = "OriginalName", IsActive = true };
        SeedTestData(
            _targetEntity,
            new TestEntity { Name = "OtherEntity", IsActive = false });

        _receivedUpdates = [];
        _initialReceived = new ManualResetEventSlim(false);
        _updateReceived = new ManualResetEventSlim(false);
    }

    void Because()
    {
        _subject = _dbContext.TestEntities.ObserveById(_targetEntity.Id);
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

        // Simulate external process updating the specific entity
        LogInfo("Updating entity directly in database...");
        UpdateDirectlyInDatabase(_targetEntity.Id, "UpdatedName");

        // Wait for notification
        _updateReceived.Wait(TimeSpan.FromSeconds(30));
    }

    [Fact] void should_receive_initial_data() => _receivedUpdates.Count.ShouldBeGreaterThanOrEqual(1);
    [Fact] void should_receive_correct_initial_entity() => _receivedUpdates[0].Name.ShouldEqual("OriginalName");
    [Fact] void should_detect_update() => _receivedUpdates[^1].Name.ShouldEqual("UpdatedName");
}
