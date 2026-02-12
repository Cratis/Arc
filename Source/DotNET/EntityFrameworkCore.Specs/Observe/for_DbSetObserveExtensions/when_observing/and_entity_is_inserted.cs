// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing;

/// <summary>
/// Specs for the Observe extension method detecting in-process INSERT operations via SaveChanges.
/// </summary>
public class and_entity_is_inserted : a_db_set_observe_context
{
    ISubject<IEnumerable<TestEntity>> _subject;
    List<IEnumerable<TestEntity>> _receivedUpdates;
    ManualResetEventSlim _initialReceived;
    ManualResetEventSlim _insertReceived;

    void Establish()
    {
        SeedTestData(new TestEntity { Name = "InitialEntity", IsActive = true });

        _receivedUpdates = [];
        _initialReceived = new ManualResetEventSlim(false);
        _insertReceived = new ManualResetEventSlim(false);
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
                    _insertReceived.Set();
                }
            }
        });

        _initialReceived.Wait(TimeSpan.FromMilliseconds(500));

        // Insert via a separate scoped DbContext to trigger ObserveInterceptor
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        dbContext.TestEntities.Add(new TestEntity { Name = "NewEntity", IsActive = true });
        dbContext.SaveChanges();

        _insertReceived.Wait(TimeSpan.FromMilliseconds(500));
    }

    [Fact] void should_receive_initial_data() => _receivedUpdates.Count.ShouldBeGreaterThanOrEqual(1);
    [Fact] void should_detect_inserted_entity() => _receivedUpdates[^1].ShouldContain(e => e.Name == "NewEntity");
    [Fact] void should_still_have_initial_entity() => _receivedUpdates[^1].ShouldContain(e => e.Name == "InitialEntity");
}
