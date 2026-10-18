// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_per_stream_models;

public class when_a_protected_stream_has_no_key_traversal : given.a_monthly_account
{
    IReadModels _readModels;
    IDecisionReads _decisionReads;
    ServiceProvider _withoutTraversal;
    IDisposable _policy;
    Exception _failure;

    void Establish()
    {
        _readModels = Substitute.For<IReadModels>();
        _decisionReads = Substitute.For<IDecisionReads>();
        _withoutTraversal = new ServiceCollection().AddSingleton(_readModels).AddSingleton(_decisionReads).BuildServiceProvider();
        _policy = DecisionPolicyForSpecs.Begin(typeof(Withdraw));
        Begin(new Withdraw(_account, "2026-10", 100), beginScope: false);
    }

    void Because() => _failure = Catch.Exception(() => PerStreamReadModelBinding.Resolve(
        typeof(MonthlyWithdrawals),
        _context,
        _withoutTraversal,
        typeof(Withdraw).GetMethod(nameof(Withdraw.Handle))!.GetParameters()[0]));

    void Destroy()
    {
        _policy.Dispose();
        _withoutTraversal.Dispose();
    }

    [Fact] void should_fail_closed_without_a_command_aware_reader() => _failure.ShouldBeOfExactType<ProtectedCommandRequiresCommandAwareReader>();
    [Fact] void should_not_read_a_model() => _readModels.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_consult_the_reader() => _decisionReads.ReceivedCalls().ShouldBeEmpty();
}
