// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity;

/// <summary>
/// Represents the verified claims of an on-behalf assertion.
/// </summary>
/// <param name="Subject">The represented user's subject, from <c>sub</c>.</param>
/// <param name="ActorSubject">The actor's subject, from <c>act.sub</c>.</param>
/// <param name="Tenant">The tenant the session acts in, from <c>tenant</c>, when present.</param>
/// <param name="PrincipalHash">The unpadded base64url SHA-256 of the represented user's principal header, from <c>principal_hash</c>.</param>
/// <param name="ActorHash">The unpadded base64url SHA-256 of the actor header, from <c>actor_hash</c>.</param>
/// <param name="Method">The HTTP method the assertion is bound to, from <c>htm</c>, when present.</param>
/// <param name="Target">The target the assertion is bound to, from <c>htu</c>, when present.</param>
/// <param name="TokenId">The unique token identifier, from <c>jti</c>.</param>
/// <param name="Expires">When the assertion expires, from <c>exp</c>.</param>
internal sealed record OnBehalfOfAssertion(
    string Subject,
    string ActorSubject,
    string? Tenant,
    string PrincipalHash,
    string ActorHash,
    string? Method,
    string? Target,
    string TokenId,
    DateTimeOffset Expires);
