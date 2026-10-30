// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Arc.Authorization;

/// <summary>
/// The ordered identity shape used by the principal fingerprint.
/// </summary>
/// <param name="IdentityType">The runtime identity type name.</param>
/// <param name="AuthenticationType">The authentication type.</param>
/// <param name="NameClaimType">The name claim type.</param>
/// <param name="RoleClaimType">The role claim type.</param>
/// <param name="Label">The identity label.</param>
/// <param name="BootstrapContext">The standard bootstrap context content.</param>
/// <param name="Actor">The recursively captured actor.</param>
/// <param name="Claims">The claims in their original order.</param>
internal sealed record PrincipalIdentityContent(
    [property: JsonPropertyOrder(0)] string? IdentityType,
    [property: JsonPropertyOrder(1)] string? AuthenticationType,
    [property: JsonPropertyOrder(2)] string NameClaimType,
    [property: JsonPropertyOrder(3)] string RoleClaimType,
    [property: JsonPropertyOrder(4)] string? Label,
    [property: JsonPropertyOrder(5)] PrincipalBootstrapContent BootstrapContext,
    [property: JsonPropertyOrder(6)] PrincipalIdentityContent? Actor,
    [property: JsonPropertyOrder(7)] IEnumerable<PrincipalClaimContent> Claims);
