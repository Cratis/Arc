// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy;

/// <summary>
/// The exception that is thrown when subdomain tenancy is configured with a base domain that cannot identify tenants.
/// </summary>
/// <param name="baseDomain">The configured base domain.</param>
/// <remarks>
/// Subdomain tenancy resolves the tenant from the request host, and falls back to a header the client sets. Without a
/// base domain to match against, that fallback would answer every request, so the configuration is refused instead.
/// </remarks>
public class BaseDomainIsNotADomainName(string baseDomain)
    : Exception($"'{baseDomain}' cannot be used as the base domain for subdomain tenancy. Set Tenancy.BaseDomain to the registrable domain the application is served from, such as 'myapp.com' - at least two labels of letters, digits and hyphens, and not an address literal. Without it every request would resolve its tenant from the client-supplied tenant header instead of from the host.")
{
    /// <summary>
    /// Throws if a base domain cannot be used to resolve tenants from a request host.
    /// </summary>
    /// <param name="baseDomain">The base domain to check.</param>
    /// <exception cref="BaseDomainIsNotADomainName">Thrown when the base domain is not a domain name.</exception>
    public static void ThrowIfNotADomainName(string baseDomain)
    {
        if (!TenantHost.IsDomainName(baseDomain))
        {
            throw new BaseDomainIsNotADomainName(baseDomain);
        }
    }
}
