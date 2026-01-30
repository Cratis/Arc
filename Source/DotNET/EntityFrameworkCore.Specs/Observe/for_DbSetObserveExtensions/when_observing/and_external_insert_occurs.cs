// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing;

/// <summary>
/// Specs for the Observe extension method detecting external INSERT operations via SQLite update_hook.
/// </summary>
public class and_external_insert_occurs : a_db_set_observe_context
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

        // Simulate external process inserting data directly into SQLite
        LogInfo("Inserting entity directly into database...");
        InsertDirectlyIntoDatabase(new TestEntity { Name = "ExternalEntity", IsActive = true });

        // Wait for notification from SQLite update_hook
        _updateReceived.Wait(TimeSpan.FromSeconds(30));
    }

    [Fact] void should_receive_initial_data() => _receivedUpdates.Count.ShouldBeGreaterThanOrEqual(1);
    [Fact] void should_detect_inserted_entity() => _receivedUpdates[^1].ShouldContain(e => e.Name == "ExternalEntity");
    [Fact] void should_still_have_initial_entity() => _receivedUpdates[^1].ShouldContain(e => e.Name == "InitialEntity");
}
