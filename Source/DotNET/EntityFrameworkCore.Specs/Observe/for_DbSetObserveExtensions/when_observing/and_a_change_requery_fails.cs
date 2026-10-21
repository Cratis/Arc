// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing;

/// <summary>
/// Specs for a change whose re-query fails: the observable must fault instead of silently dropping the change.
/// </summary>
public class and_a_change_requery_fails : a_db_set_observe_context
{
    ISubject<IEnumerable<TestEntity>> _subject;
    List<IEnumerable<TestEntity>> _received;
    Exception _error;
    Exception _callerException;
    int _emissionsAfterError;
    ManualResetEventSlim _initialReceived;
    ManualResetEventSlim _errorReceived;

    void Establish()
    {
        SeedTestData(new TestEntity { Name = "InitialEntity", IsActive = true });

        _received = [];
        _initialReceived = new ManualResetEventSlim(false);
        _errorReceived = new ManualResetEventSlim(false);
    }

    void Because()
    {
        _subject = _dbContext.TestEntities.Observe();
        _subject.Subscribe(
            entities =>
            {
                if (_errorReceived.IsSet)
                {
                    Interlocked.Increment(ref _emissionsAfterError);
                    return;
                }

                _received.Add(entities.ToList());
                _initialReceived.Set();
            },
            error =>
            {
                _error = error;
                _errorReceived.Set();
            });

        _initialReceived.Wait(TimeSpan.FromSeconds(2));

        _queryFailureInterceptor.Arm();
        try
        {
            InsertDirectlyIntoDatabase(new TestEntity { Name = "NewEntity", IsActive = true });
        }
        catch (Exception ex)
        {
            _callerException = ex;
        }

        _errorReceived.Wait(TimeSpan.FromSeconds(2));
        _queryFailureInterceptor.Disarm();

        // A change arriving after the fault must be ignored by the ended observation.
        InsertDirectlyIntoDatabase(new TestEntity { Name = "LateEntity", IsActive = true });
        Thread.Sleep(300);
    }

    [Fact] void should_fault_the_observable() => _error.ShouldNotBeNull();
    [Fact] void should_fault_with_the_requery_failure() => _error.ToString().ShouldContain("Simulated re-query failure");
    [Fact] void should_not_throw_into_the_save_changes_caller() => _callerException.ShouldBeNull();
    [Fact] void should_not_emit_after_the_fault() => _emissionsAfterError.ShouldEqual(0);
    [Fact] void should_only_have_emitted_the_initial_state() => _received.Count.ShouldEqual(1);
}
