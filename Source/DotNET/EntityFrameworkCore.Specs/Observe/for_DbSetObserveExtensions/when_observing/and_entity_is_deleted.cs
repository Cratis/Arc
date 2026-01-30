// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing;

/// <summary>
/// Specs for the Observe extension method detecting in-process DELETE operations via SaveChanges.
/// </summary>
public class and_entity_is_deleted : a_db_set_observe_context
{
    ISubject<IEnumerable<TestEntity>> _subject;
    List<IEnumerable<TestEntity>> _receivedUpdates;
    ManualResetEventSlim _initialReceived;
    ManualResetEventSlim _deleteReceived;
    TestEntity _entityToDelete;
    TestEntity _entityToKeep;

    void Establish()
    {
        _entityToDelete = new TestEntity { Name = "Entity1", IsActive = true };
        _entityToKeep = new TestEntity { Name = "Entity2", IsActive = true };
        SeedTestData(_entityToDelete, _entityToKeep);

        _receivedUpdates = [];
        _initialReceived = new ManualResetEventSlim(false);
        _deleteReceived = new ManualResetEventSlim(false);
    }

    void Because()
    {
        _subject = _dbContext.TestEntities.Observe();
        _subject.Subscribe(entities =>
        {
            var list = entities.ToList();
            _receivedUpdates.Add(list);

            if (!_initialReceived.IsSet && list.Count == 2)
            {
                _initialReceived.Set();
            }
            else if (_initialReceived.IsSet && list.Count == 1)
            {
                _deleteReceived.Set();
            }
        });

        _initialReceived.Wait(TimeSpan.FromMilliseconds(500));

        // Delete via helper method that uses a separate scoped DbContext
        DeleteDirectlyFromDatabase(_entityToDelete.Id);

        _deleteReceived.Wait(TimeSpan.FromMilliseconds(500));
    }

    [Fact] void should_receive_initial_data_with_two_entities() => _receivedUpdates.First(u => u.Count() == 2).Count().ShouldEqual(2);
    [Fact] void should_detect_deletion() => _receivedUpdates[^1].Count().ShouldEqual(1);
    [Fact] void should_still_have_remaining_entity() => _receivedUpdates[^1].ShouldContain(e => e.Name == "Entity2");
    [Fact] void should_not_have_deleted_entity() => _receivedUpdates[^1].ShouldNotContain(e => e.Name == "Entity1");
}
