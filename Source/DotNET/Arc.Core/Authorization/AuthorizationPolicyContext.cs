// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;

namespace Cratis.Arc.Authorization;

/// <summary>
/// The caller and resource presented to an authorization policy.
/// </summary>
/// <param name="Principal">The principal selected by the authorization schemes.</param>
/// <param name="Target">The command type or query method being authorized.</param>
/// <param name="Resource">The command or query context.</param>
public record AuthorizationPolicyContext(ClaimsPrincipal Principal, MemberInfo Target, object Resource)
{
    /// <summary>
    /// Gets the time Arc received the operation being authorized, not the network arrival time.
    /// A hand-constructed context has <see langword="default"/> unless the caller sets this property.
    /// </summary>
    public DateTimeOffset ReceivedAt { get; init; }
}
