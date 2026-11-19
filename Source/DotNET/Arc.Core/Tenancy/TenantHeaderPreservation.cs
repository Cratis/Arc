// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Checks header-based tenancy before the HttpListener host accepts requests.
/// </summary>
internal static class TenantHeaderPreservation
{
    /// <summary>
    /// Refuses strict header tenancy, or warns about the host's duplicate-header limitation.
    /// </summary>
    /// <param name="services">The application's services.</param>
    /// <param name="logger">The host logger.</param>
    /// <remarks>
    /// Like HttpListenerRequestContext's credential-header preservation check, this treats HttpListener as unable to
    /// guarantee duplicate preservation. Earlier field lines may already be lost before Arc can inspect a request.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Duplicate-header preservation is required for header tenancy.</exception>
    internal static void ValidateHttpListener(IServiceProvider services, ILogger logger)
    {
        var resolver = services.GetService<ITenantIdResolver>();
        if (resolver is not HeaderTenantIdResolver and not SubdomainTenantIdResolver)
        {
            return;
        }

        var tenancy = services.GetRequiredService<IOptions<ArcOptions>>().Value.Tenancy;
        if (tenancy.RequireDuplicateHeaderPreservation)
        {
            throw new InvalidOperationException("HttpListener cannot guarantee preservation of repeated tenant-header lines. Use the ASP.NET Core host, or set Cratis:Arc:Tenancy:RequireDuplicateHeaderPreservation=false only behind a trusted ingress that strips all incoming tenant-header occurrences and injects exactly one value, with direct backend access prevented.");
        }

        logger.DuplicateTenantHeadersNotPreserved(tenancy.HttpHeader);
    }
}
