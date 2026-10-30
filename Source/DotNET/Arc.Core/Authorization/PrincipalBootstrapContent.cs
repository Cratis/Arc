// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Arc.Authorization;

/// <summary>
/// The ordered bootstrap context shape used by the principal fingerprint.
/// </summary>
/// <param name="IsOpaque">Whether the context is a non-string object.</param>
/// <param name="Value">The context when it is a string.</param>
internal sealed record PrincipalBootstrapContent(
    [property: JsonPropertyOrder(0)] bool IsOpaque,
    [property: JsonPropertyOrder(1)] string? Value);
