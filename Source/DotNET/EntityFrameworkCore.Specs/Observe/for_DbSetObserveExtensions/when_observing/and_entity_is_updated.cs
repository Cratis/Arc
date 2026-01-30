// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing;

/// <summary>
/// Specs for the Observe extension method detecting in-process UPDATE operations via SaveChanges.
/// </summary>
public class and_entity_is_updated : a_db_set_observe_context
{
    ISubject<IEnumerable<TestEntity>> _subject;
    List<IEnumerable<TestEntity>> _receivedUpdates;
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
                else if (list.Exists(e => e.Name == "UpdatedName"))
                {
                    _updateReceived.Set();
                }
            }
        });

        _initialReceived.Wait(TimeSpan.FromMilliseconds(500));

        // Update via a separate scoped DbContext to trigger ObserveInterceptor
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var entity = dbContext.TestEntities.Find(_seededEntity.Id);
        if (entity is not null)
        {
            entity.Name = "UpdatedName";
            dbContext.SaveChanges();
        }

        _updateReceived.Wait(TimeSpan.FromMilliseconds(500));
    }

    [Fact] void should_receive_initial_data() => _receivedUpdates.Count.ShouldBeGreaterThanOrEqual(1);
    [Fact] void should_detect_updated_entity() => _receivedUpdates[^1].ShouldContain(e => e.Name == "UpdatedName");
}
