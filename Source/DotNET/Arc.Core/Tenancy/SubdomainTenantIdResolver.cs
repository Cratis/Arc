// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy;

/// <summary>
/// Represents an implementation of <see cref="ITenantIdResolver"/> that resolves the tenant ID from the request
/// subdomain, with the configured HTTP header as fallback.
/// </summary>
/// <param name="httpRequestContextAccessor">The <see cref="IHttpRequestContextAccessor"/>.</param>
/// <param name="options">The <see cref="IOptions{TOptions}"/>.</param>
/// <exception cref="BaseDomainIsNotADomainName">
/// Thrown when <see cref="TenancyOptions.BaseDomain"/> is not a domain name tenants can be resolved in front of.
/// </exception>
/// <remarks>
/// A host carries a tenant only when it is exactly one label in front of the configured
/// <see cref="TenancyOptions.BaseDomain"/> - <c>acme.myapp.com</c> resolves <c>acme</c> for the base domain
/// <c>myapp.com</c>. The base domain itself, a deeper host, an IP literal, an unrelated host and anything that is not
/// a valid DNS label fall back to <see cref="TenancyOptions.HttpHeader"/>. The number of labels in a host says nothing
/// about whether it carries a tenant, so it is never used to decide.
/// </remarks>
[IgnoreConvention]
public class SubdomainTenantIdResolver(IHttpRequestContextAccessor httpRequestContextAccessor, IOptions<ArcOptions> options) : ITenantIdResolver
{
    readonly string _suffix = SuffixFor(options.Value.Tenancy.BaseDomain);

    /// <inheritdoc/>
    public string Resolve()
    {
        var context = httpRequestContextAccessor.Current;
        if (context is null)
        {
            return string.Empty;
        }

        var tenantId = ResolveFromHost(context.Host);
        if (tenantId.Length > 0)
        {
            return tenantId;
        }

        var tenancy = options.Value.Tenancy;
        if (string.IsNullOrWhiteSpace(tenancy.HttpHeader))
        {
            return string.Empty;
        }

        return context.Headers.TryGetValue(tenancy.HttpHeader, out var fallbackTenantId) ? fallbackTenantId : string.Empty;
    }

    /// <summary>
    /// Builds the suffix a tenant host is matched against from the base domain subdomain tenancy is configured with.
    /// </summary>
    /// <param name="baseDomain">The configured base domain.</param>
    /// <returns>The suffix a tenant host ends with.</returns>
    /// <exception cref="BaseDomainIsNotADomainName">Thrown when the base domain is not a domain name.</exception>
    /// <remarks>
    /// The suffix is built once, from the base domain the guard accepted, and never rebuilt. Normalizing the base
    /// domain on every request would be pure repetition for a singleton, and rebuilding it from whatever
    /// <see cref="TenancyOptions.BaseDomain"/> holds at the time would let a base domain written after construction
    /// past the guard - the resolver would silently stop matching hosts and hand every request to the client-supplied
    /// header instead. <see cref="IOptions{TOptions}"/> is a snapshot, so there is nothing to rebuild for anyway.
    /// </remarks>
    static string SuffixFor(string baseDomain)
    {
        BaseDomainIsNotADomainName.ThrowIfNotADomainName(baseDomain);
        return $".{TenantHost.Normalize(baseDomain)}";
    }

    string ResolveFromHost(string host)
    {
        var normalizedHost = TenantHost.Normalize(host);
        if (!normalizedHost.EndsWith(_suffix, StringComparison.Ordinal))
        {
            return string.Empty;
        }

        var subdomain = normalizedHost[..^_suffix.Length];
        return TenantHost.IsLabel(subdomain) ? subdomain : string.Empty;
    }
}
