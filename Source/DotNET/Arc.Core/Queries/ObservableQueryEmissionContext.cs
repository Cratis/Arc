// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Execution;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the context handed to an <see cref="IGuardObservableQueryEmission"/> for a single emission on an
/// observable query subscription.
/// </summary>
/// <param name="QueryName">The <see cref="FullyQualifiedQueryName"/> the subscription was established for.</param>
/// <param name="Arguments">The coerced <see cref="QueryArguments"/> the subscription was established with.</param>
/// <param name="Principal">The <see cref="ClaimsPrincipal"/> of the caller that established the subscription, or null when the subscription is anonymous.</param>
/// <param name="CorrelationId">The <see cref="CorrelationId"/> the subscription stamps every emission with.</param>
/// <param name="ServiceProvider">The per-subscription <see cref="IServiceProvider"/> to resolve guard dependencies from.</param>
/// <param name="IsFirstEmission">Whether this is the first emission delivered on the subscription.</param>
/// <param name="CancellationToken">The <see cref="CancellationToken"/> that is cancelled when the subscription ends.</param>
/// <remarks>
/// The principal is passed explicitly rather than resolved ambiently. Emissions arrive on the producing stream's own
/// thread, where the request's <c>AsyncLocal</c> context does not flow, so a guard that reached for an ambient accessor
/// would see whichever identity — or none — that thread happened to carry.
/// <para>
/// For a WebSocket subscription the principal is the one captured when the socket was upgraded: the WebSocket protocol
/// offers no way to re-present credentials on an established connection, so it does not change for the life of that
/// connection. A guard that needs a fresher verdict must reach its own source of truth (a session store, a token
/// introspection endpoint, a revocation list) using the identity carried here.
/// </para>
/// </remarks>
public record ObservableQueryEmissionContext(
    FullyQualifiedQueryName QueryName,
    QueryArguments Arguments,
    ClaimsPrincipal? Principal,
    CorrelationId CorrelationId,
    IServiceProvider ServiceProvider,
    bool IsFirstEmission,
    CancellationToken CancellationToken);
