// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing;

/// <summary>
/// Specs for the Observe extension method detecting in-process changes for schema-qualified entities.
/// Proves that the schema-qualified key is used correctly throughout the notification chain:
/// DbSetObserveExtensions registers "testschema.SchemaTestEntities" as the key,
/// and ObserveInterceptor emits the same key when changes are saved.
/// </summary>
public class with_schema_and_entity_is_inserted : a_schema_db_set_observe_context
{
    ISubject<IEnumerable<SchemaTestEntity>> _subject;
    List<IEnumerable<SchemaTestEntity>> _receivedUpdates;
    ManualResetEventSlim _initialReceived;
    ManualResetEventSlim _insertReceived;

    void Establish()
    {
        SeedTestData(new SchemaTestEntity { Name = "InitialEntity" });

        _receivedUpdates = [];
        _initialReceived = new ManualResetEventSlim(false);
        _insertReceived = new ManualResetEventSlim(false);
    }

    void Because()
    {
        _subject = _dbContext.SchemaTestEntities.Observe();
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
                    _insertReceived.Set();
                }
            }
        });

        _initialReceived.Wait(TimeSpan.FromMilliseconds(500));

        InsertDirectlyIntoDatabase(new SchemaTestEntity { Name = "NewSchemaEntity" });

        _insertReceived.Wait(TimeSpan.FromMilliseconds(500));
    }

    [Fact] void should_receive_initial_data() => _receivedUpdates.Count.ShouldBeGreaterThanOrEqual(1);
    [Fact] void should_detect_inserted_entity() => _receivedUpdates[^1].ShouldContain(e => e.Name == "NewSchemaEntity");
    [Fact] void should_still_have_initial_entity() => _receivedUpdates[^1].ShouldContain(e => e.Name == "InitialEntity");
}
