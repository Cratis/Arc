// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_per_stream_models;

public class when_a_protected_stream_has_no_key_traversal : given.a_monthly_account
{
    [Fact]
    public void should_fail_closed_before_reading()
    {
        var readModels = Substitute.For<IReadModels>();
        var decisionReads = Substitute.For<IDecisionReads>();
        using var withoutTraversal = new ServiceCollection().AddSingleton(readModels).AddSingleton(decisionReads).BuildServiceProvider();
        using var policy = DecisionPolicyForSpecs.Begin(typeof(Withdraw));
        Begin(new Withdraw(_account, "2026-10", 100), beginScope: false);

        var failure = Catch.Exception(() => PerStreamReadModelBinding.Resolve(
            typeof(MonthlyWithdrawals),
            _context,
            withoutTraversal,
            typeof(Withdraw).GetMethod(nameof(Withdraw.Handle))!.GetParameters()[0]));

        failure.ShouldBeOfExactType<ProtectedCommandRequiresCommandAwareReader>();
        readModels.ReceivedCalls().ShouldBeEmpty();
        decisionReads.ReceivedCalls().ShouldBeEmpty();
    }
}
