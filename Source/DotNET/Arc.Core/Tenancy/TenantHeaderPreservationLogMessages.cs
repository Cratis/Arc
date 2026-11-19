// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Holds startup diagnostics for tenant-header preservation.
/// </summary>
internal static partial class TenantHeaderPreservationLogMessages
{
    /// <summary>
    /// Warns that HttpListener may discard conflicting tenant-header lines before resolution.
    /// </summary>
    /// <param name="logger">The host logger.</param>
    /// <param name="header">The configured tenant header.</param>
    [LoggerMessage(LogLevel.Warning, "HttpListener cannot guarantee rejection of conflicting repeated {Header} tenant-header lines; earlier lines may be discarded before Arc sees them. Configure a trusted ingress to strip ALL incoming tenant-header occurrences and inject exactly one value, and prevent direct backend access. Set Cratis:Arc:Tenancy:RequireDuplicateHeaderPreservation=true to refuse this host, or use ASP.NET Core.")]
    internal static partial void DuplicateTenantHeadersNotPreserved(this ILogger logger, string header);
}
