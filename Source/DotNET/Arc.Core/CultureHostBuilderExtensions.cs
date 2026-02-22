// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for <see cref="IHostBuilder"/> for configuring culture settings.
/// </summary>
public static class CultureHostBuilderExtensions
{
    /// <summary>
    /// Configure the application to use invariant culture for all culture-sensitive operations.
    /// </summary>
    /// <param name="builder"><see cref="IHostBuilder"/> to extend.</param>
    /// <returns><see cref="IHostBuilder"/> for building continuation.</returns>
    /// <remarks>
    /// This sets both DefaultThreadCurrentCulture and DefaultThreadCurrentUICulture to InvariantCulture,
    /// ensuring consistent behavior across all threads and culture-sensitive operations such as:
    /// DateTime formatting and parsing, number formatting and parsing, string comparisons and sorting,
    /// and resource string lookups.
    /// </remarks>
    public static IHostBuilder UseInvariantCulture(this IHostBuilder builder)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        return builder;
    }
}
