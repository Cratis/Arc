// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides extension methods for <see cref="WebApplicationBuilder"/> for configuring Arc services.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Adds Cratis Arc — commands, queries, validation, tenancy, and C# → TypeScript proxy generation — to the
    /// <see cref="WebApplicationBuilder"/>. On its own this wires Arc with no event store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Binds the <see cref="ArcOptions"/> configuration to the given config section path or the default
    /// Cratis:Arc section path. Pair it with <c>UseCratisArc</c> on the built application.
    /// </para>
    /// <para>
    /// Use this directly to back commands and queries with MongoDB or EF Core instead of an event log. To add
    /// event sourcing, chain <c>WithChronicle</c> via <paramref name="configureBuilder"/> (or use <c>AddCratis</c>,
    /// which composes Arc, the Chronicle client, and identity in one call). Calling this without
    /// <c>WithChronicle</c> while a command or query depends on a Chronicle service such as <c>IEventLog</c> fails
    /// resolution with a message that points you at the fix.
    /// </para>
    /// </remarks>
    /// <param name="builder"><see cref="WebApplicationBuilder"/> to extend.</param>
    /// <param name="configureOptions">The optional callback for configuring <see cref="ArcOptions"/>.</param>
    /// <param name="configureBuilder">Callback for configuring the <see cref="IArcBuilder"/>.</param>
    /// <param name="configSectionPath">The optional configuration section path.</param>
    /// <returns><see cref="WebApplicationBuilder"/> for building continuation.</returns>
    /// <example>
    /// Arc on its own (back commands and queries with MongoDB or EF Core instead of an event store):
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    ///
    /// builder.AddCratisArc();
    ///
    /// var app = builder.Build();
    ///
    /// app.UseCratisArc();
    /// app.Run();
    /// </code>
    /// Arc plus the Chronicle event store, bringing your own authentication (what <c>AddCratis</c> does minus identity):
    /// <code>
    /// builder.AddCratisArc(configureBuilder: arc => arc.WithChronicle());
    ///
    /// var app = builder.Build();
    ///
    /// app.UseCratisArc();
    /// app.UseCratisChronicle();
    /// app.Run();
    /// </code>
    /// </example>
    public static WebApplicationBuilder AddCratisArc(
        this WebApplicationBuilder builder,
        Action<ArcOptions>? configureOptions = default,
        Action<IArcBuilder>? configureBuilder = default,
        string? configSectionPath = default)
    {
        var configSection = configSectionPath ?? ConfigurationPath.Combine(Cratis.Arc.HostBuilderExtensions.DefaultSectionPaths);
        builder.Services.Configure<ArcOptions>(builder.Configuration.GetSection(configSection));

        builder.Services.AddOptions<ArcOptions>()
            .ValidateOnStart();

        if (configureOptions is not null)
        {
            builder.Services.PostConfigure(configureOptions);
        }

        builder.Services.AddCratisArcCore();
        builder.Services.AddIdentityProvider();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddControllersFromProjectReferencedAssembles(Internals.Types);

        builder.Host.SkipEagerServiceProviderValidation();
        builder.AddCorrelationIdLogEnricher();

        if (configureBuilder is not null)
        {
            var arcBuilder = new ArcBuilder(builder, Internals.Types);
            configureBuilder.Invoke(arcBuilder);
        }

        return builder;
    }
}