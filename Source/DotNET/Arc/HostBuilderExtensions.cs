// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for <see cref="IHostBuilder"/> for configuring Arc services.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Gets the default section name for Arc configuration.
    /// </summary>
    public static readonly string[] DefaultArcSectionPaths = ["Cratis", "Arc"];

    /// <summary>
    /// Use Cratis Arc with the <see cref="IHostBuilder"/>.
    /// </summary>
    /// <remarks>
    /// Binds the <see cref="ArcOptions"/> configuration to the given config section path or the default
    /// Cratis:Arc section path.
    /// </remarks>
    /// <param name="builder"><see cref="IHostBuilder"/> to extend.</param>
    /// <param name="configureOptions">The optional callback for configuring <see cref="ArcOptions"/>.</param>
    /// <param name="configureBuilder">Optional callback for configuring the <see cref="IArcBuilder"/>.</param>
    /// <param name="configSectionPath">The optional configuration section path.</param>
    /// <returns><see cref="IHostBuilder"/> for building continuation.</returns>
    public static IHostBuilder AddCratisArc(
        this IHostBuilder builder,
        Action<ArcOptions>? configureOptions = default,
        Action<IArcBuilder>? configureBuilder = default,
        string? configSectionPath = default)
    {
        builder.AddCratisArcCore(configureOptions, configureBuilder, configSectionPath);
        builder.AddArcImplementation();

        return builder;
    }

    static IHostBuilder AddArcImplementation(this IHostBuilder builder, Type? identityDetailsProvider = default)
    {
        builder.UseDefaultServiceProvider(_ => _.ValidateOnBuild = false);
        builder.AddCorrelationIdLogEnricher();

        builder
            .ConfigureServices(services =>
            {
                services.AddHttpContextAccessor();
                services.AddControllersFromProjectReferencedAssembles(Internals.Types);

                if (identityDetailsProvider is not null)
                {
                    services.AddIdentityProvider(identityDetailsProvider);
                }
                else
                {
                    services.AddIdentityProvider(Internals.Types);
                }
            });

        return builder;
    }
}
