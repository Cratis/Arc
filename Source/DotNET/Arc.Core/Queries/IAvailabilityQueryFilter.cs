// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines a query filter that decides whether a query is available, before authorization.
/// </summary>
/// <remarks>
/// It is an <see cref="IAuthorizationQueryFilter"/>, so it runs in the authorization stage, and the query filters run it
/// before the other authorization filters: a query in a part that is not available is refused with
/// <see cref="Availability.AvailabilityReason.NotAvailable"/> before <c>authorize</c> is evaluated.
/// </remarks>
public interface IAvailabilityQueryFilter : IAuthorizationQueryFilter;
