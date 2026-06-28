// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;
using FluentValidation;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

/// <summary>
/// A read model materialized from ledger events by a reducer, used to verify the events-based
/// read model seeding path of a command scenario.
/// </summary>
public record LedgerBalance
{
    /// <summary>
    /// Gets the current balance.
    /// </summary>
    public decimal Balance { get; init; }
}

/// <summary>
/// The reducer that materializes <see cref="LedgerBalance"/> from ledger events.
/// </summary>
public class LedgerBalanceReducer : IReducerFor<LedgerBalance>
{
    public LedgerBalance Credited(LedgerCredited @event, LedgerBalance? current) =>
        new() { Balance = (current?.Balance ?? 0m) + @event.Amount };

    public LedgerBalance Debited(LedgerDebited @event, LedgerBalance? current) =>
        new() { Balance = (current?.Balance ?? 0m) - @event.Amount };
}

[Command]
public record SettleLedger(EventSourceId EventSourceId)
{
    public LedgerSettled Handle(LedgerBalance balance) => new(balance.Balance);
}

public class SettleLedgerValidator : CommandValidator<SettleLedger>
{
    public SettleLedgerValidator(LedgerBalance balance) =>
        RuleFor(command => command.EventSourceId)
            .Must(_ => balance.Balance > 0)
            .WithMessage("Ledger has no funds to settle.");
}

/// <summary>
/// A read model materialized from ledger events by a reducer, but without a parameterless constructor — used to
/// verify that the events-based seeding path materializes a read model that cannot be created by convention.
/// </summary>
/// <param name="Balance">The current balance.</param>
public record StatementBalance(decimal Balance);

/// <summary>
/// The reducer that materializes <see cref="StatementBalance"/> from ledger events.
/// </summary>
public class StatementBalanceReducer : IReducerFor<StatementBalance>
{
    public StatementBalance Credited(LedgerCredited @event, StatementBalance? current) =>
        new((current?.Balance ?? 0m) + @event.Amount);
}

[Command]
public record SettleStatement(EventSourceId EventSourceId)
{
    public LedgerSettled Handle(StatementBalance balance) => new(balance.Balance);
}

[Command]
public record SummarizeLedger(EventSourceId EventSourceId)
{
    public LedgerSummarized Handle(LedgerBalance balance, StatementBalance statement) => new(balance.Balance, statement.Balance);
}

[EventType("1a2b3c4d-5e6f-4a7b-8c9d-0e1f2a3b4c5d")]
public record LedgerCredited(decimal Amount);

[EventType("4d5e6f70-8192-4d0e-9f1a-2b3c4d5e6f80")]
public record LedgerSummarized(decimal BalanceTotal, decimal StatementTotal);

[EventType("2b3c4d5e-6f70-4b8c-9d0e-1f2a3b4c5d6e")]
public record LedgerDebited(decimal Amount);

[EventType("3c4d5e6f-7081-4c9d-0e1f-2a3b4c5d6e7f")]
public record LedgerSettled(decimal Balance);
