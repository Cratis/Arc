// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using FluentValidation;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

[Command]
public record WithdrawFunds(EventSourceId EventSourceId)
{
    public FundsWithdrawn Handle() => new();
}

public class WithdrawFundsValidator : CommandValidator<WithdrawFunds>
{
    public WithdrawFundsValidator(AccountBalanceReadModel account) =>
        RuleFor(command => command.EventSourceId)
            .Must(_ => account.Balance > 0)
            .WithMessage("Account has insufficient funds.");
}

[EventType("b2e6f3a1-4c7d-4e2a-9f10-2a3b4c5d6e7f")]
public record FundsWithdrawn;
