// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.EntityFrameworkCore.SqlServer.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.SqlServer.Observe.for_DbSetObserveExtensions.when_observing_single;

/// <summary>
/// Specs for the ObserveSingle extension method returning initial data.
/// </summary>
/// <param name="fixture">The SQL Server fixture.</param>
[Collection(SqlServerCollection.Name)]
public class and_entity_exists(SqlServerFixture fixture) : a_sql_server_observe_context(fixture)
{
    ISubject<TestEntity> _subject;
    TestEntity _receivedEntity;
    ManualResetEventSlim _receivedEvent;

    void Establish()
    {
        SeedTestData(
            new TestEntity { Name = "TargetEntity", IsActive = true },
            new TestEntity { Name = "OtherEntity", IsActive = false });

        _receivedEvent = new ManualResetEventSlim(false);
    }

    void Because()
    {
        _subject = _dbContext.TestEntities.ObserveSingle(e => e.IsActive);
        _subject.Subscribe(entity =>
        {
            _receivedEntity = entity;
            _receivedEvent.Set();
        });

        _receivedEvent.Wait(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_return_matching_entity() => _receivedEntity.ShouldNotBeNull();
    [Fact] void should_return_active_entity() => _receivedEntity.Name.ShouldEqual("TargetEntity");
}
