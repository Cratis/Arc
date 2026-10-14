// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario;

/// <summary>
/// An order placed against an account on a separate event source.
/// </summary>
/// <param name="AccountId">The account to join.</param>
/// <param name="Amount">The order amount.</param>
[EventType]
public record ObservedOrderPlaced(string AccountId, decimal Amount);
