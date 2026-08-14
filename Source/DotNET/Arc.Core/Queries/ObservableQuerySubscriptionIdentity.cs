// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Queries;

/// <summary>
/// Captures what identifies a multiplexed observable query subscription for the lifetime of its stream.
/// </summary>
/// <param name="QueryName">The <see cref="FullyQualifiedQueryName"/> the subscription was established for.</param>
/// <param name="Arguments">The coerced <see cref="QueryArguments"/> the subscription was established with.</param>
/// <param name="Principal">The <see cref="ClaimsPrincipal"/> of the caller that established the subscription.</param>
/// <remarks>
/// Emissions arrive on the producing stream's own thread, where the request's <c>AsyncLocal</c> context does not flow,
/// so anything an emission needs about the caller has to be captured at subscribe time and carried explicitly.
/// </remarks>
internal sealed record ObservableQuerySubscriptionIdentity(
    FullyQualifiedQueryName QueryName,
    QueryArguments Arguments,
    ClaimsPrincipal? Principal);
