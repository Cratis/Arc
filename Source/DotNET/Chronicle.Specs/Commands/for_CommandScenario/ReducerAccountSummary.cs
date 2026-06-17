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
/// A read model materialized by a reducer (not a projection or model-bound projection).
/// </summary>
/// <param name="Balance">The account balance.</param>
/// <param name="IsFrozen">Whether the account is frozen.</param>
public record ReducerAccountSummary(decimal Balance, bool IsFrozen);

/// <summary>
/// The reducer that materializes <see cref="ReducerAccountSummary"/>.
/// </summary>
public class ReducerAccountSummaryReducer : IReducerFor<ReducerAccountSummary>;

[Command]
public record WithdrawFromReducerAccount(EventSourceId EventSourceId)
{
    public ReducerFundsWithdrawn Handle() => new();
}

public class WithdrawFromReducerAccountValidator : CommandValidator<WithdrawFromReducerAccount>
{
    public WithdrawFromReducerAccountValidator(ReducerAccountSummary summary) =>
        RuleFor(command => command.EventSourceId)
            .Must(_ => !summary.IsFrozen)
            .WithMessage("Account is frozen.");
}

[Command]
public record UseReducerReadModelInHandle(EventSourceId EventSourceId)
{
    public ReducerBalanceUsedInHandle Handle(ReducerAccountSummary summary) => new(summary.Balance);
}

[Command]
public record UseReducerReadModelInProvide(EventSourceId EventSourceId)
{
    public ProvidedReducerBalance Provide(ReducerAccountSummary summary) => new(summary.Balance);

    public ReducerBalanceUsedInProvide Handle(ProvidedReducerBalance balance) => new(balance.Value);
}

[Command]
public record UseNullableReducerReadModelInHandle(EventSourceId EventSourceId)
{
    public ReducerReadModelAbsence Handle(ReducerAccountSummary? summary) => new(summary is null);
}

public record ProvidedReducerBalance(decimal Value);

[EventType("c1d2e3f4-0a1b-2c3d-4e5f-60718293a4b5")]
public record ReducerFundsWithdrawn;

[EventType("d2e3f4a5-1b2c-3d4e-5f60-718293a4b5c6")]
public record ReducerBalanceUsedInHandle(decimal Balance);

[EventType("e3f4a5b6-2c3d-4e5f-6071-8293a4b5c6d7")]
public record ReducerBalanceUsedInProvide(decimal Balance);

[EventType("f4a5b6c7-3d4e-5f60-7182-93a4b5c6d7e8")]
public record ReducerReadModelAbsence(bool WasAbsent);
