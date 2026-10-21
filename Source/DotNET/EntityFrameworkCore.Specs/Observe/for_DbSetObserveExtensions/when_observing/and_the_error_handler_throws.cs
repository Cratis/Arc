// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing;

/// <summary>
/// Specs for a faulted observation whose error subscriber throws: the failure must stay contained and the observation must still end.
/// </summary>
public class and_the_error_handler_throws : a_db_set_observe_context
{
    ISubject<IEnumerable<TestEntity>> _subject;
    Exception _callerException;
    ManualResetEventSlim _initialReceived;

    void Establish()
    {
        SeedTestData(new TestEntity { Name = "InitialEntity", IsActive = true });
        _initialReceived = new ManualResetEventSlim(false);
    }

    void Because()
    {
        _subject = _dbContext.TestEntities.Observe();
        _subject.Subscribe(
            _ => _initialReceived.Set(),
            _ => throw new InvalidOperationException("Observer failed"));
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

        InsertDirectlyIntoDatabase(new TestEntity { Name = "LateEntity", IsActive = true });
    }

    [Fact] void should_not_throw_into_the_save_changes_caller() => _callerException.ShouldBeNull();
    [Fact] void should_not_run_a_query_for_a_change_after_the_fault() => _queryFailureInterceptor.Failures.ShouldEqual(1);
}
