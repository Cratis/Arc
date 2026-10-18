// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.Queries;

/// <summary>
/// Shares the direct endpoint's anonymous access contract with query hub admission.
/// </summary>
internal static class QueryEndpointAccess
{
    /// <summary>
    /// Determines whether host fallback authorization is deferred to the query pipeline.
    /// </summary>
    /// <param name="performer">The query performer.</param>
    /// <param name="services">The host services used to resolve guest policy declarations.</param>
    /// <returns>Whether the query allows anonymous endpoint access.</returns>
    internal static bool AllowsAnonymous(IQueryPerformer performer, IServiceProvider services) =>
        performer.AllowsAnonymousAccess || GuestPolicyEndpointAccess.ForQuery(performer, services) || QueryHealthEndpointAccess.ForQuery(performer, services);
}
