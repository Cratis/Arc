// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Arc.Authorization;

/// <summary>
/// The ordered key/value shape used by claim properties in the principal fingerprint.
/// </summary>
/// <param name="Key">The property key.</param>
/// <param name="Value">The property value.</param>
internal sealed record PrincipalClaimProperty(
    [property: JsonPropertyOrder(0)] string Key,
    [property: JsonPropertyOrder(1)] string? Value);
