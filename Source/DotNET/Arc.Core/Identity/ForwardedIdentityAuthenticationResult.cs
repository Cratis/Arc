// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Identity;

/// <summary>
/// Represents a completed forwarded authentication, a value-free failure, or an absent credential.
/// </summary>
/// <param name="Principal">The independent authenticated principal, if successful.</param>
/// <param name="Failure">A fixed rejection code, if rejected.</param>
internal sealed record ForwardedIdentityAuthenticationResult(ClaimsPrincipal? Principal = null, string? Failure = null);
