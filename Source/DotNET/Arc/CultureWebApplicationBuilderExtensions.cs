// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Microsoft.AspNetCore.Localization;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides extension methods for <see cref="WebApplicationBuilder"/> for configuring culture settings.
/// </summary>
public static class CultureWebApplicationBuilderExtensions
{
    /// <summary>
    /// Configure the application to use invariant culture for all culture-sensitive operations.
    /// </summary>
    /// <param name="builder"><see cref="WebApplicationBuilder"/> to extend.</param>
    /// <returns><see cref="WebApplicationBuilder"/> for building continuation.</returns>
    /// <remarks>
    /// This configures both the host and request pipeline to use InvariantCulture.
    /// Sets DefaultThreadCurrentCulture and DefaultThreadCurrentUICulture to InvariantCulture
    /// and configures request localization to always use InvariantCulture.
    /// This ensures consistent behavior across all threads and culture-sensitive operations such as
    /// DateTime formatting and parsing, number formatting and parsing, string comparisons and sorting,
    /// and resource string lookups.
    /// </remarks>
    public static WebApplicationBuilder UseInvariantCulture(this WebApplicationBuilder builder)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        builder.Services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture);
            options.SupportedCultures = [CultureInfo.InvariantCulture];
            options.SupportedUICultures = [CultureInfo.InvariantCulture];
            options.RequestCultureProviders.Clear();
        });

        return builder;
    }
}
