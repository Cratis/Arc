// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.EntityFrameworkCore.PostgreSql.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.PostgreSql.Observe.for_DbSetObserveExtensions.when_observing_with_schema;

/// <summary>
/// Verifies that an external insert into a schema-qualified table fires the observer
/// via LISTEN/NOTIFY using the correct schema-qualified trigger.
/// </summary>
/// <param name="fixture">The PostgreSQL fixture.</param>
[Collection(PostgreSqlCollection.Name)]
public class and_external_insert_occurs(PostgreSqlFixture fixture) : a_postgresql_schema_observe_context(fixture)
{
    ISubject<IEnumerable<SchemaEntityA>> _subject;
    List<IEnumerable<SchemaEntityA>> _receivedUpdates;
    ManualResetEventSlim _initialReceived;
    ManualResetEventSlim _insertReceived;

    void Establish()
    {
        SeedSchemaA(new SchemaEntityA { Name = "InitialEntity", IsActive = true });

        _receivedUpdates = [];
        _initialReceived = new ManualResetEventSlim(false);
        _insertReceived = new ManualResetEventSlim(false);
    }

    void Because()
    {
        _subject = _dbContextA.SchemaEntitiesA.Observe();
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

        _initialReceived.Wait(TimeSpan.FromSeconds(10));

        LogInfo("Inserting entity directly into schema_a...");
        InsertIntoSchemaA(new SchemaEntityA { Name = "ExternalSchemaEntity", IsActive = true });

        _insertReceived.Wait(TimeSpan.FromSeconds(30));
    }

    [Fact] void should_receive_initial_data() => _receivedUpdates.Count.ShouldBeGreaterThanOrEqual(1);
    [Fact] void should_detect_inserted_entity() => _receivedUpdates[^1].ShouldContain(e => e.Name == "ExternalSchemaEntity");
    [Fact] void should_still_have_initial_entity() => _receivedUpdates[^1].ShouldContain(e => e.Name == "InitialEntity");
}
