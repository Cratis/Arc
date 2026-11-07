// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// Supplies the cookie names of the registered cookie authentication schemes as additional session cookie names.
/// The names are read once, on first use, after startup configuration is complete; any failure leaves only the fixed set in force.
/// </summary>
/// <param name="serviceProvider">The service provider used to resolve the scheme provider and options lazily.</param>
internal sealed class CookieSchemeSessionCookieNames(IServiceProvider serviceProvider) : IAdditionalSessionCookieNames
{
    readonly Lazy<string[]> _names = new(() => Read(serviceProvider));

    /// <inheritdoc/>
    public IReadOnlyCollection<string> Names => _names.Value;

    static string[] Read(IServiceProvider serviceProvider)
    {
        try
        {
            var schemes = serviceProvider.GetService<IAuthenticationSchemeProvider>();
            var options = serviceProvider.GetService<IOptionsMonitor<CookieAuthenticationOptions>>();
            if (schemes is null || options is null)
            {
                return [];
            }

            return schemes.GetAllSchemesAsync().GetAwaiter().GetResult()
                .Where(scheme => typeof(CookieAuthenticationHandler).IsAssignableFrom(scheme.HandlerType))
                .Select(scheme => options.Get(scheme.Name).Cookie.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (Exception)
        {
            // Fail closed: the fixed set still applies.
            return [];
        }
    }
}
