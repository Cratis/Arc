// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver.when_binding_asynchronously;

public class and_the_read_is_released_as_soon_as_it_is_entered : given.a_gated_protected_read
{
    public class TokenHandler
    {
        public void Handle(DecisionRead<Balance> balance) { }
    }

    ParameterInfo _parameter;
    ChronicleParameterDependencyResolver _resolver;
    object? _token;
    Exception _failure;

    void Establish()
    {
        _openOnEntry = true;
        _parameter = typeof(TokenHandler).GetMethod(nameof(TokenHandler.Handle))!.GetParameters()[0];
        _resolver = new([typeof(Balance)], (_, token) => token == typeof(DecisionRead<Balance>));
    }

    async Task Because()
    {
        var run = _scheduler.Run(async () =>
        {
            using var policy = for_CommandDecisionReads.DecisionPolicyForSpecs.Begin(typeof(Transfer));
            CommandTransaction.Current = _unit;
            using var frame = _unit.EnforceReadProtection();
            CommandDecisionReads.Begin(typeof(Transfer));
            try
            {
                return (await _resolver.Resolve(_services, _parameter)).Dependency;
            }
            finally
            {
                CommandDecisionReads.End();
            }
        });
        _failure = await Catch.Exception(async () => _token = await run.WaitAsync(_bound, TestContext.Current.CancellationToken));
    }

    [Fact] void should_not_fail() => _failure.ShouldBeNull();
    [Fact] void should_issue_a_protected_token_for_the_command_key() => ((DecisionRead<Balance>)_token!).IsProtected.ShouldBeTrue();
}
