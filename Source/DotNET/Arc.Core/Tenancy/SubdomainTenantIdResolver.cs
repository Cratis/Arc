// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Net;
using System.Text;
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
/// <remarks>
/// A host carries a tenant only when it is exactly one label in front of the configured
/// <see cref="TenancyOptions.BaseDomain"/> - <c>acme.myapp.com</c> resolves <c>acme</c> for the base domain
/// <c>myapp.com</c>. The base domain itself, a deeper host, an IP literal, an unrelated host and every host at all
/// when no base domain is configured fall back to <see cref="TenancyOptions.HttpHeader"/>. The number of labels in a
/// host says nothing about whether it carries a tenant, so it is never used to decide.
/// </remarks>
[IgnoreConvention]
public class SubdomainTenantIdResolver(IHttpRequestContextAccessor httpRequestContextAccessor, IOptions<ArcOptions> options) : ITenantIdResolver
{
    static readonly IdnMapping _idnMapping = new();

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

    static string ResolveFromHost(string host, string baseDomain)
    {
        if (string.IsNullOrWhiteSpace(baseDomain))
        {
            return string.Empty;
        }

        var suffix = $".{Normalize(baseDomain)}";
        var normalizedHost = Normalize(host);
        if (!normalizedHost.EndsWith(suffix, StringComparison.Ordinal))
        {
            return string.Empty;
        }

        var subdomain = normalizedHost[..^suffix.Length];
        return subdomain.Contains('.') ? string.Empty : subdomain;
    }

    /// <summary>
    /// Reduces a host to the canonical form the base domain is matched against, or to an empty string for a host that
    /// can never carry a tenant.
    /// </summary>
    /// <param name="value">The host to normalize.</param>
    /// <returns>The normalized host, or an empty string when the host cannot carry a tenant.</returns>
    /// <remarks>
    /// The bracketed-literal and <see cref="IPAddress"/> rejections state the contract explicitly rather than leaving
    /// it to the base domain match, so an address can never be read as a tenant even if the matching rule changes.
    /// </remarks>
    static string Normalize(string value)
    {
        var host = HostName.WithoutPort(value.Trim());
        if (host.Length == 0)
        {
            return string.Empty;
        }

        if (host[0] == '[')
        {
            return string.Empty;
        }

        if (IPAddress.TryParse(host, out _))
        {
            return string.Empty;
        }

        var withoutTrailingDots = host.Trim('.');
        var lowercased = withoutTrailingDots.ToLowerInvariant();
        return ToAscii(lowercased);
    }

    /// <summary>
    /// Converts an internationalized host to its punycode form so the same domain always yields the same tenant ID.
    /// </summary>
    /// <param name="host">The host to convert.</param>
    /// <returns>The punycode form, or an empty string when the host is not a valid internationalized domain name.</returns>
    static string ToAscii(string host)
    {
        if (Ascii.IsValid(host))
        {
            return host;
        }

        try
        {
            return _idnMapping.GetAscii(host);
        }
        catch (ArgumentException)
        {
            // Not a domain name at all, so it identifies no tenant and the header fallback takes over.
            return string.Empty;
        }
    }
}
