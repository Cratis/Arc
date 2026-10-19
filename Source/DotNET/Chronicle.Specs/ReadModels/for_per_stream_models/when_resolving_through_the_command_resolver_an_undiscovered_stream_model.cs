// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels.for_per_stream_models;

public class when_resolving_through_the_command_resolver_an_undiscovered_stream_model : given.a_monthly_account
{
    Exception _failure;

    void Establish() => Begin(new Withdraw(_account, "2026-10", 100), beginScope: false);

    void Because() => _failure = Catch.Exception(() => new ChronicleReadModelForCommandResolver([typeof(UndiscoveredWithdrawals)])
        .Resolve(typeof(UndiscoveredWithdrawals), _context).GetAwaiter().GetResult());

    [Fact] void should_not_fail_for_missing_dispatch() => (_failure is MissingReadModelDispatch).ShouldBeFalse();

    [EventStream<BankAccount>("Transactions")]
    public record UndiscoveredWithdrawals(AccountId Id, decimal Total);
}
