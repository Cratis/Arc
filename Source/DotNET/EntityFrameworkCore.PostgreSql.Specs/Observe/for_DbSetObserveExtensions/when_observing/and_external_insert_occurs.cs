// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.EntityFrameworkCore.PostgreSql.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.PostgreSql.Observe.for_DbSetObserveExtensions.when_observing;

/// <summary>
/// Specs for the Observe extension method detecting external INSERT operations via LISTEN/NOTIFY.
/// </summary>
/// <param name="fixture">The PostgreSQL fixture.</param>
[Collection(PostgreSqlCollection.Name)]
public class and_external_insert_occurs(PostgreSqlFixture fixture) : a_postgresql_observe_context(fixture)
{
    ISubject<IEnumerable<TestEntity>> _subject;
    List<IEnumerable<TestEntity>> _receivedUpdates;
    ManualResetEventSlim _initialReceived;
    ManualResetEventSlim _updateReceived;

    void Establish()
    {
        SeedTestData(
            new TestEntity { Name = "InitialEntity", IsActive = true });

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
            if (list.Count > 0)
            {
                _receivedUpdates.Add(list);

                if (!_initialReceived.IsSet)
                {
                    _initialReceived.Set();
                }
                else if (list.Count > 1)
                {
                    // We received an update with more than the initial entity
                    _updateReceived.Set();
                }
            }
        });

        // Wait for initial data
        _initialReceived.Wait(TimeSpan.FromSeconds(10));

        // Simulate external process inserting data directly into PostgreSQL
        LogInfo("Inserting entity directly into database...");
        InsertDirectlyIntoDatabase(new TestEntity { Name = "ExternalEntity", IsActive = true });

        // Wait for notification from LISTEN/NOTIFY
        _updateReceived.Wait(TimeSpan.FromSeconds(30));
    }

    [Fact] void should_receive_initial_data() => _receivedUpdates.Count.ShouldBeGreaterThanOrEqual(1);
    [Fact] void should_detect_inserted_entity() => _receivedUpdates[^1].ShouldContain(e => e.Name == "ExternalEntity");
    [Fact] void should_still_have_initial_entity() => _receivedUpdates[^1].ShouldContain(e => e.Name == "InitialEntity");
}
