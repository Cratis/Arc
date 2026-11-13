// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver.when_binding_asynchronously;

public class and_a_decision_read_is_issued_for_a_framework_registered_token : given.a_gated_protected_read
{
    public class TokenHandler
    {
        public void Handle(DecisionRead<Balance> balance) { }
    }

    ParameterInfo _parameter;
    ChronicleParameterDependencyResolver _resolver;
    ValueTask<Arc.DependencyInjection.ParameterDependencyResolution> _pending;
    bool _completedBeforeRelease;
    object? _token;
    Exception _failure;

    void Establish()
    {
        _parameter = typeof(TokenHandler).GetMethod(nameof(TokenHandler.Handle))!.GetParameters()[0];
        _resolver = new([typeof(Balance)], (_, token) => token == typeof(DecisionRead<Balance>));
    }

    async Task Because()
    {
        var completionObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var run = _scheduler.Run(async () =>
        {
            using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(Transfer));
            CommandTransaction.Current = _unit;
            using var frame = _unit.EnforceReadProtection();
            CommandDecisionReads.Begin(typeof(Transfer));
            try
            {
                _pending = _resolver.Resolve(_services, _parameter);
                _completedBeforeRelease = _pending.IsCompleted;
                completionObserved.TrySetResult();
                return (await _pending).Dependency;
            }
            finally
            {
                CommandDecisionReads.End();
            }
        });
        _failure = await Catch.Exception(async () =>
        {
            // The gate must stay closed until _completedBeforeRelease is captured: releasing it as soon as
            // the read is entered (the shared ReleaseAfterEntry) races the asynchronous continuation that
            // opens the gate against this check, which occasionally let the read complete before IsCompleted
            // was read.
            await completionObserved.Task.WaitAsync(_bound, TestContext.Current.CancellationToken);
            _gate.SetResult();
            _token = await run.WaitAsync(_bound, TestContext.Current.CancellationToken);
        });
    }

    [Fact] void should_not_fail() => _failure.ShouldBeNull();
    [Fact] void should_yield_while_the_read_is_pending() => _completedBeforeRelease.ShouldBeFalse();
    [Fact] void should_issue_a_protected_token_for_the_command_key() => ((DecisionRead<Balance>)_token!).IsProtected.ShouldBeTrue();
}
