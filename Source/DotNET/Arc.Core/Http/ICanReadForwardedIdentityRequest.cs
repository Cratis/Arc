// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http;

/// <summary>
/// Exposes host-observed transport inputs for a single signed forwarded identity snapshot.
/// </summary>
/// <remarks>
/// The verifier copies each relevant header once and requires exactly one observed value; commas remain data.
/// Managed HttpListener exposes only the last observed value when a wire header is repeated. Its earlier values
/// cannot be recovered by this capability. ASP.NET Core exposes multiple observed values, which are rejected.
/// Windows/http.sys wire cardinality has not been verified. Custom hosts must expose their observed values and
/// the escaped origin-form target honestly; flattened dictionaries and decoded paths are not substitutes.
/// MAC verification and identity construction must use the same copied snapshot, without rereading the host.
/// </remarks>
public interface ICanReadForwardedIdentityRequest
{
    /// <summary>
    /// Gets the host-observed header names, compared case-insensitively.
    /// </summary>
    IEnumerable<string> HeaderNames { get; }

    /// <summary>
    /// Gets the original origin-form request target, including escaped path and query, or null when unavailable.
    /// </summary>
    string? RawTarget { get; }

    /// <summary>
    /// Gets the host-observed field values for a case-insensitive header name, without further flattening.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <returns>The observed values, or an empty list for an absent header. This is not a promise of wire cardinality.</returns>
    IReadOnlyList<string> GetHeaderValues(string name);
}
