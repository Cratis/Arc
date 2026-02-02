// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.EntityFrameworkCore.PostgreSql.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.PostgreSql.Observe.for_DbSetObserveExtensions.when_observing;

/// <summary>
/// Specs for the Observe extension method detecting external DELETE operations via LISTEN/NOTIFY.
/// </summary>
/// <param name="fixture">The PostgreSQL fixture.</param>
[Collection(PostgreSqlCollection.Name)]
public class and_external_delete_occurs(PostgreSqlFixture fixture) : a_postgresql_observe_context(fixture)
{
    ISubject<IEnumerable<TestEntity>> _subject;
    List<IEnumerable<TestEntity>> _receivedUpdates;
    ManualResetEventSlim _initialReceived;
    ManualResetEventSlim _updateReceived;
    TestEntity _entityToDelete;
    TestEntity _entityToKeep;

    void Establish()
    {
        _entityToDelete = new TestEntity { Name = "Entity1", IsActive = true };
        _entityToKeep = new TestEntity { Name = "Entity2", IsActive = true };
        SeedTestData(_entityToDelete, _entityToKeep);

        _receivedUpdates = [];
        _initialReceived = new ManualResetEventSlim(false);
        _updateReceived = new ManualResetEventSlim(false);
    }

    void Because()
    {
        _subject = _dbContext.TestEntities.Observe();
        _subject.Subscribe(entities =>
        {
            var list = entities.ToList();
            _receivedUpdates.Add(list);

            // Wait for initial data with 2 entities (skip empty initial emission from BehaviorSubject)
            if (!_initialReceived.IsSet && list.Count == 2)
            {
                _initialReceived.Set();
            }
            else if (_initialReceived.IsSet && list.Count == 1)
            {
                _updateReceived.Set();
            }
        });

        // Wait for initial data
        _initialReceived.Wait(TimeSpan.FromSeconds(10));

        // Simulate external process deleting data directly from PostgreSQL
        LogInfo("Deleting entity directly from database...");
        DeleteDirectlyFromDatabase(_entityToDelete.Id);

        // Wait for notification from LISTEN/NOTIFY
        _updateReceived.Wait(TimeSpan.FromSeconds(30));
    }

    [Fact] void should_receive_initial_data_with_two_entities() => _receivedUpdates.First(u => u.Count() == 2).Count().ShouldEqual(2);
    [Fact] void should_detect_deletion() => _receivedUpdates[^1].Count().ShouldEqual(1);
    [Fact] void should_still_have_remaining_entity() => _receivedUpdates[^1].ShouldContain(e => e.Name == "Entity2");
    [Fact] void should_not_have_deleted_entity() => _receivedUpdates[^1].ShouldNotContain(e => e.Name == "Entity1");
}
