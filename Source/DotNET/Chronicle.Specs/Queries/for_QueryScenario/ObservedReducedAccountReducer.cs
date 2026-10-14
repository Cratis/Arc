// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario;

/// <summary>
/// Accumulates account changes to distinguish reduction from projection.
/// </summary>
public class ObservedReducedAccountReducer : IReducerFor<ObservedReducedAccount>
{
    /// <summary>
    /// Applies an account change to the current balance.
    /// </summary>
    /// <param name="event">The balance change.</param>
    /// <param name="current">The current account, if any.</param>
    /// <param name="context">The event context supplying the account identifier.</param>
    /// <returns>The updated account.</returns>
    public ObservedReducedAccount Changed(ObservedAccountChanged @event, ObservedReducedAccount? current, EventContext context) =>
        new(context.EventSourceId.Value, (current?.Balance ?? 0m) + @event.Balance);
}
