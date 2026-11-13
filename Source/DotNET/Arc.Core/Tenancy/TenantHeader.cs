// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Resolves a tenant only when all nonempty values in its HTTP header agree.
/// </summary>
internal static class TenantHeader
{
    /// <summary>
    /// Resolves the tenant from agreeing, comma-separated header values.
    /// </summary>
    /// <param name="headers">The request headers.</param>
    /// <param name="headerName">The configured tenant header name.</param>
    /// <returns>The single tenant ID, or an empty string if values are missing or conflicting.</returns>
    internal static string Resolve(IReadOnlyDictionary<string, string> headers, string headerName)
    {
        if (!headers.TryGetValue(headerName, out var value))
        {
            return string.Empty;
        }

        var tenantIds = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return tenantIds.Length == 1 ? tenantIds[0] : string.Empty;
    }
}
