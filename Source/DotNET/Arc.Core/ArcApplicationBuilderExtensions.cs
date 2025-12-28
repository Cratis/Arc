// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc;

/// <summary>
/// Extension methods for <see cref="ArcApplicationBuilder"/>.
/// </summary>
public static class ArcApplicationBuilderExtensions
{
    /// <summary>
    /// Adds Cratis Arc services to the application.
    /// </summary>
    /// <param name="builder">The <see cref="ArcApplicationBuilder"/>.</param>
    /// <param name="configureOptions">Optional callback for configuring <see cref="ArcOptions"/>.</param>
    /// <param name="configureBuilder">Optional callback for configuring the <see cref="IArcBuilder"/>.</param>
    /// <param name="configSectionPath">The optional configuration section path.</param>
    /// <returns>The <see cref="ArcApplicationBuilder"/> for continuation.</returns>
    public static ArcApplicationBuilder AddCratisArc(
        this ArcApplicationBuilder builder,
        Action<ArcOptions>? configureOptions = null,
        Action<IArcBuilder>? configureBuilder = null,
        string? configSectionPath = null)
    {
        builder.Services.AddCratisArcCore();

        var configSection = configSectionPath ?? ConfigurationPath.Combine(HostBuilderExtensions.DefaultSectionPaths);
        builder.Services.Configure<ArcOptions>(builder.Configuration.GetSection(configSection));

        var optionsBuilder = builder.Services
            .AddOptions<ArcOptions>()
            .ValidateOnStart();

        if (configureOptions is not null)
        {
            optionsBuilder.Configure(configureOptions);
        }

        if (configureBuilder is not null)
        {
            var arcBuilder = new ArcBuilder(builder.Services, Internals.Types);
            configureBuilder.Invoke(arcBuilder);
        }

        builder.Services.AddSingleton<Http.IHttpRequestContextAccessor, Http.HttpRequestContextAccessor>();
        builder.Services.AddTransient<IObservableQueryHandler, StreamingQueryHandler>();

        var arcOptions = builder.Configuration.GetSection(configSection).Get<ArcOptions>();
        if (arcOptions?.IdentityDetailsProvider is not null)
        {
            builder.Services.AddIdentityProvider(arcOptions.IdentityDetailsProvider);
        }
        else
        {
            builder.Services.AddIdentityProvider(Internals.Types);
        }

        return builder;
    }
}
