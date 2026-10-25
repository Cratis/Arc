// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.EntityFrameworkCore.SqlServer.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.SqlServer.Observe.for_DbSetObserveExtensions.when_observing_with_schema;

/// <summary>
/// Verifies that two observers on different SQL Server schemas are fully isolated:
/// an external insert into schema_a fires only the schema_a observer,
/// leaving the schema_b observer unaffected until its own positive-control insert.
/// </summary>
/// <param name="fixture">The SQL Server fixture.</param>
[Collection(SqlServerCollection.Name)]
public class and_two_schemas_are_segregated(SqlServerFixture fixture) : a_sql_server_schema_observe_context(fixture)
{
    ISubject<IEnumerable<SchemaEntityA>> _subjectA;
    ISubject<IEnumerable<SchemaEntityB>> _subjectB;
    List<IEnumerable<SchemaEntityA>> _updatesA;
    List<IEnumerable<SchemaEntityB>> _updatesB;
    ManualResetEventSlim _initialAReceived;
    ManualResetEventSlim _initialBReceived;
    ManualResetEventSlim _insertIntoAReceived;
    ManualResetEventSlim _insertIntoBReceived;
    int _schemaBUpdatesBeforeInsert;

    void Establish()
    {
        SeedSchemaA(new SchemaEntityA { Name = "InitialA", IsActive = true });
        SeedSchemaB(new SchemaEntityB { Name = "InitialB", IsActive = true });

        _updatesA = [];
        _updatesB = [];
        _initialAReceived = new ManualResetEventSlim(false);
        _initialBReceived = new ManualResetEventSlim(false);
        _insertIntoAReceived = new ManualResetEventSlim(false);
        _insertIntoBReceived = new ManualResetEventSlim(false);
    }

    void Because()
    {
        _subjectA = _dbContextA.SchemaEntitiesA.Observe();
        _subjectA.Subscribe(entities =>
        {
            var list = entities.ToList();
            _updatesA.Add(list);

            if (!_initialAReceived.IsSet && list.Count > 0)
            {
                _initialAReceived.Set();
            }
            else if (_initialAReceived.IsSet && list.Count > 1)
            {
                _insertIntoAReceived.Set();
            }
        });

        _subjectB = _dbContextB.SchemaEntitiesB.Observe();
        _subjectB.Subscribe(entities =>
        {
            var list = entities.ToList();
            _updatesB.Add(list);

            if (!_initialBReceived.IsSet && list.Count > 0)
            {
                _initialBReceived.Set();
            }
            else if (_initialBReceived.IsSet && list.Exists(entity => entity.Name == "NewEntityB"))
            {
                _insertIntoBReceived.Set();
            }
        });

        _initialAReceived.Wait(TimeSpan.FromSeconds(10));
        _initialBReceived.Wait(TimeSpan.FromSeconds(10));

        // Insert only into schema_a — schema_b observer must not fire
        LogInfo("Inserting entity directly into schema_a only...");
        InsertIntoSchemaA(new SchemaEntityA { Name = "NewEntityA", IsActive = true });

        _insertIntoAReceived.Wait(TimeSpan.FromSeconds(30));

        _schemaBUpdatesBeforeInsert = _updatesB.Count;

        // A positive-control insert proves schema_b's observer has delivered a later notification.
        InsertIntoSchemaB(new SchemaEntityB { Name = "NewEntityB", IsActive = true });
        _insertIntoBReceived.Wait(TimeSpan.FromSeconds(30)).ShouldBeTrue();
    }

    void Destroy()
    {
        _subjectA?.OnCompleted();
        _subjectB?.OnCompleted();
    }

    [Fact] void should_notify_observer_for_schema_a() => _updatesA.Count.ShouldBeGreaterThan(1);
    [Fact] void should_detect_inserted_entity_in_schema_a() => _updatesA[^1].ShouldContain(e => e.Name == "NewEntityA");
    [Fact] void should_not_notify_observer_for_schema_b() => _schemaBUpdatesBeforeInsert.ShouldEqual(1);
    [Fact] void should_keep_initial_entity_in_schema_b() => _updatesB[^1].ShouldContain(e => e.Name == "InitialB");
    [Fact] void should_only_notify_schema_b_for_its_own_insert() => _updatesB.Count.ShouldEqual(2);
    [Fact] void should_detect_inserted_entity_in_schema_b() => _updatesB[^1].ShouldContain(e => e.Name == "NewEntityB");
}
