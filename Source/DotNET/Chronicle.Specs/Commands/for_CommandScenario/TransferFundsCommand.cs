// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario;

/// <summary>
/// A command that returns a single <see cref="EventForEventSourceId"/>, appending a debit event to the from-account.
/// </summary>
/// <param name="FromAccountId">The event source ID of the debit account.</param>
/// <param name="ToAccountId">The event source ID of the credit account.</param>
/// <param name="Amount">The amount to transfer.</param>
[Command]
public record TransferFundsCommand(EventSourceId FromAccountId, EventSourceId ToAccountId, decimal Amount)
{
    /// <summary>
    /// Handles the command by returning a debit event for the from-account event source.
    /// </summary>
    /// <returns>An <see cref="EventForEventSourceId"/> targeting the from account.</returns>
    public EventForEventSourceId Handle() =>
        new(FromAccountId, new FundsDebited(Amount));
}
