// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http;

/// <summary>
/// Exposes the unflattened transport inputs required to verify signed forwarded identity.
/// </summary>
/// <remarks>Custom hosts must preserve field-value cardinality and the escaped origin-form target; decoded paths are insufficient.</remarks>
public interface ICanReadForwardedIdentityRequest
{
    /// <summary>
    /// Gets the transport header names without combining repeated field values.
    /// </summary>
    IEnumerable<string> HeaderNames { get; }

    /// <summary>
    /// Gets the original origin-form request target, including escaped path and query, or null when unavailable.
    /// </summary>
    string? RawTarget { get; }

    /// <summary>
    /// Gets the raw field values for a case-insensitive header name, preserving each occurrence.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <returns>The raw values, or an empty list for an absent header.</returns>
    IReadOnlyList<string> GetHeaderValues(string name);
}
