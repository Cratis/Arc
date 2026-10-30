// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Arc.Authorization;

/// <summary>
/// The ordered claim shape used by the principal fingerprint.
/// </summary>
/// <param name="Type">The claim type.</param>
/// <param name="Value">The claim value.</param>
/// <param name="ValueType">The claim value type.</param>
/// <param name="Issuer">The issuer.</param>
/// <param name="OriginalIssuer">The original issuer.</param>
/// <param name="Properties">The claim properties ordered by ordinal key.</param>
internal sealed record PrincipalClaimContent(
    [property: JsonPropertyOrder(0)] string Type,
    [property: JsonPropertyOrder(1)] string Value,
    [property: JsonPropertyOrder(2)] string ValueType,
    [property: JsonPropertyOrder(3)] string Issuer,
    [property: JsonPropertyOrder(4)] string OriginalIssuer,
    [property: JsonPropertyOrder(5)] IEnumerable<PrincipalClaimProperty> Properties);
