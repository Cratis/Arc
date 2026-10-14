// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario;

/// <summary>
/// An order enriched with the balance of its account through a cross-source join.
/// </summary>
/// <param name="Id">The order identifier.</param>
/// <param name="AccountId">The account identifier used for the join.</param>
/// <param name="Amount">The order amount.</param>
/// <param name="Balance">The joined account balance.</param>
[ReadModel]
[FromEvent<ObservedOrderPlaced>]
public record ObservedOrder(
    [property: Key]
    [SetFromContext<ObservedOrderPlaced>(nameof(EventContext.EventSourceId))]
    string Id,
    string AccountId,
    decimal Amount,
    [Join<ObservedAccountChanged>(on: nameof(AccountId), eventPropertyName: nameof(ObservedAccountChanged.Balance))]
    decimal Balance)
{
    /// <summary>
    /// Observes all orders with their joined account state.
    /// </summary>
    /// <param name="readModels">The scenario read models.</param>
    /// <returns>The order collection stream.</returns>
    [AllowAnonymous]
    public static ISubject<IEnumerable<ObservedOrder>> All(IReadModels readModels) => readModels.Observe<ObservedOrder>();
}
