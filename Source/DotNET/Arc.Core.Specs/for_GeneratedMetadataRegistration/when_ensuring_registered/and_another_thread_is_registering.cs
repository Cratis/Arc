// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.for_GeneratedMetadataRegistration.when_ensuring_registered;

public class and_another_thread_is_registering : given.a_generated_metadata_registration
{
    static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

    readonly ManualResetEventSlim _firstIsRegistering = new(false);
    readonly ManualResetEventSlim _releaseFirst = new(false);
    InvalidOperationException _failure;
    Exception _firstError;
    Exception _secondError;
    bool _firstFinishedWhenSecondReturned;
    volatile bool _firstFinished;

    void Establish()
    {
        _failure = new("Registration failed");
        _registration.Register(_entryAssembly, Modules(("Failing.Project", FailWhenReleased)), []);
    }

    void Because()
    {
        var first = new Thread(() => _firstError = Catch.Exception(_registration.EnsureRegistered));
        var second = new Thread(() =>
        {
            _secondError = Catch.Exception(_registration.EnsureRegistered);
            _firstFinishedWhenSecondReturned = _firstFinished;
        });

        first.Start();
        _firstIsRegistering.Wait(_timeout);
        second.Start();

        // The second caller is either blocked on the first, or, if it does not wait for it, has already returned.
        SpinWait.SpinUntil(() => second.ThreadState.HasFlag(ThreadState.WaitSleepJoin) || second.ThreadState.HasFlag(ThreadState.Stopped), _timeout);
        _releaseFirst.Set();

        first.Join(_timeout);
        second.Join(_timeout);
    }

    void Destroy()
    {
        _firstIsRegistering.Dispose();
        _releaseFirst.Dispose();
    }

    Module FailWhenReleased()
    {
        _firstIsRegistering.Set();
        _releaseFirst.Wait(_timeout);
        _firstFinished = true;
        throw _failure;
    }

    [Fact] void should_fail_the_first_caller() => _firstError.ShouldEqual(_failure);
    [Fact] void should_not_return_to_the_second_caller_before_the_first_has_finished() => _firstFinishedWhenSecondReturned.ShouldBeTrue();
    [Fact] void should_fail_the_second_caller_with_the_failure_of_the_first() => _secondError.ShouldEqual(_failure);
}
