// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
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
    /// <param name="arcBuilderCallback">Optional callback for configuring the <see cref="IArcBuilder"/>.</param>
    /// <param name="configSectionPath">The optional configuration section path.</param>
    /// <returns>The <see cref="ArcApplicationBuilder"/> for continuation.</returns>
    public static ArcApplicationBuilder AddCratisArc(
        this ArcApplicationBuilder builder,
        Action<IArcBuilder>? arcBuilderCallback = null,
        string? configSectionPath = null)
    {
        var configSection = configSectionPath ?? "Cratis:Arc";
        builder.Services.Configure<ArcOptions>(builder.Configuration.GetSection(configSection));

        builder.Services.AddOptions<ArcOptions>()
            .ValidateOnStart();

        builder.Services.AddCratisArcCore();
        builder.Services.AddSingleton<Http.IHttpRequestContextAccessor, Http.HttpRequestContextAccessor>();
        builder.Services.AddTransient<IObservableQueryHandler, StreamingQueryHandler>();

        if (arcBuilderCallback is not null)
        {
            var arcBuilder = new ArcBuilder(builder.Services, Internals.Types);
            arcBuilderCallback(arcBuilder);
        }

        return builder;
    }
}
