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
    BaseDomainSuffix _suffix = BaseDomainSuffix.ForConfigured(options.Value.Tenancy.BaseDomain);

    /// <inheritdoc/>
    public string Resolve()
    {
        var context = httpRequestContextAccessor.Current;
        if (context is null)
        {
            return string.Empty;
        }

        var tenancy = options.Value.Tenancy;
        var tenantId = ResolveFromHost(context.Host, tenancy.BaseDomain);
        if (tenantId.Length > 0)
        {
            return tenantId;
        }

        if (string.IsNullOrWhiteSpace(tenancy.HttpHeader))
        {
            return string.Empty;
        }

        return context.Headers.TryGetValue(tenancy.HttpHeader, out var fallbackTenantId) ? fallbackTenantId : string.Empty;
    }

    string ResolveFromHost(string host, string baseDomain)
    {
        var suffix = SuffixFor(baseDomain);
        if (suffix.Length == 0)
        {
            return string.Empty;
        }

        var normalizedHost = TenantHost.Normalize(host);
        if (!normalizedHost.EndsWith(suffix, StringComparison.Ordinal))
        {
            return string.Empty;
        }

        var subdomain = normalizedHost[..^suffix.Length];
        return TenantHost.IsLabel(subdomain) ? subdomain : string.Empty;
    }

    string SuffixFor(string baseDomain)
    {
        var cached = _suffix;
        if (cached.WasBuiltFor(baseDomain))
        {
            return cached.Suffix;
        }

        var rebuilt = BaseDomainSuffix.For(baseDomain);
        _suffix = rebuilt;
        return rebuilt.Suffix;
    }

    /// <summary>
    /// Represents the suffix a tenant host is matched against, remembered for the base domain it was built from.
    /// </summary>
    /// <param name="BaseDomain">The base domain the suffix was built from.</param>
    /// <param name="Suffix">The suffix a tenant host ends with, or an empty string when the base domain matches nothing.</param>
    /// <remarks>
    /// Normalizing the base domain and allocating the suffix on every request is pure repetition, and the resolver is
    /// a singleton, so the pair is kept as one immutable value that can be swapped in a single reference assignment.
    /// </remarks>
    sealed record BaseDomainSuffix(string BaseDomain, string Suffix)
    {
        /// <summary>
        /// Builds the suffix for the base domain subdomain tenancy was configured with.
        /// </summary>
        /// <param name="baseDomain">The configured base domain.</param>
        /// <returns>The <see cref="BaseDomainSuffix"/> for the base domain.</returns>
        /// <exception cref="BaseDomainIsNotADomainName">Thrown when the base domain is not a domain name.</exception>
        internal static BaseDomainSuffix ForConfigured(string baseDomain)
        {
            BaseDomainIsNotADomainName.ThrowIfNotADomainName(baseDomain);
            return For(baseDomain);
        }

        /// <summary>
        /// Builds the suffix for a base domain.
        /// </summary>
        /// <param name="baseDomain">The base domain to build the suffix for.</param>
        /// <returns>The <see cref="BaseDomainSuffix"/> for the base domain.</returns>
        internal static BaseDomainSuffix For(string baseDomain)
        {
            var normalized = TenantHost.Normalize(baseDomain);
            return new(baseDomain, normalized.Length == 0 ? string.Empty : $".{normalized}");
        }

        /// <summary>
        /// Checks whether the suffix was built from a base domain.
        /// </summary>
        /// <param name="baseDomain">The base domain to check against.</param>
        /// <returns>True when the suffix was built from the base domain, false otherwise.</returns>
        internal bool WasBuiltFor(string baseDomain) => string.Equals(BaseDomain, baseDomain, StringComparison.Ordinal);
    }
}
