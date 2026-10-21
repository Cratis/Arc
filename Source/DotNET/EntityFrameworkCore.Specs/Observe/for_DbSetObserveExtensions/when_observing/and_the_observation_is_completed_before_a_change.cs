// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing;

/// <summary>
/// Specs for a change arriving after the observation was torn down: teardown must stay quiet and never fault.
/// </summary>
public class and_the_observation_is_completed_before_a_change : a_db_set_observe_context
{
    ISubject<IEnumerable<TestEntity>> _subject;
    Exception _error;
    bool _completed;
    ManualResetEventSlim _initialReceived;

    void Establish()
    {
        SeedTestData(new TestEntity { Name = "InitialEntity", IsActive = true });
        _initialReceived = new ManualResetEventSlim(false);
    }

    void Because()
    {
        _subject = _dbContext.TestEntities.Observe();
        _subject.Subscribe(_ => _initialReceived.Set(), error => _error = error, () => _completed = true);
        _initialReceived.Wait(TimeSpan.FromSeconds(2));

        _queryFailureInterceptor.Arm();
        _subject.OnCompleted();

        InsertDirectlyIntoDatabase(new TestEntity { Name = "NewEntity", IsActive = true });
        _queryFailureInterceptor.Disarm();
    }

    [Fact] void should_not_fault_the_observable() => _error.ShouldBeNull();
    [Fact] void should_have_completed() => _completed.ShouldBeTrue();
}
