// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.EntityFrameworkCore.PostgreSql.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.PostgreSql.Observe.for_DbSetObserveExtensions.when_observing;

/// <summary>
/// Specs for the Observe extension method returning initial data.
/// </summary>
/// <param name="fixture">The PostgreSQL fixture.</param>
[Collection(PostgreSqlCollection.Name)]
public class and_database_has_entities(PostgreSqlFixture fixture) : a_postgresql_observe_context(fixture)
{
    ISubject<IEnumerable<TestEntity>> _subject;
    IEnumerable<TestEntity> _receivedEntities;
    ManualResetEventSlim _receivedEvent;

    void Establish()
    {
        SeedTestData(
            new TestEntity { Name = "Entity1", IsActive = true, SortOrder = 1 },
            new TestEntity { Name = "Entity2", IsActive = true, SortOrder = 2 },
            new TestEntity { Name = "Entity3", IsActive = false, SortOrder = 3 });

        _receivedEvent = new ManualResetEventSlim(false);
        _receivedEntities = [];
    }

    void Because()
    {
        _subject = _dbContext.TestEntities.Observe();
        _subject.Subscribe(entities =>
        {
            var list = entities.ToList();
            if (list.Count > 0 || _receivedEvent.IsSet)
            {
                _receivedEntities = list;
                _receivedEvent.Set();
            }
        });

        _receivedEvent.Wait(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_return_all_entities() => _receivedEntities.Count().ShouldEqual(3);
    [Fact] void should_include_first_entity() => _receivedEntities.ShouldContain(e => e.Name == "Entity1");
    [Fact] void should_include_second_entity() => _receivedEntities.ShouldContain(e => e.Name == "Entity2");
    [Fact] void should_include_third_entity() => _receivedEntities.ShouldContain(e => e.Name == "Entity3");
}
