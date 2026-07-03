// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Chronicle.Tenancy;
using Cratis.Chronicle.AspNetCore;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides extension methods for <see cref="WebApplicationBuilder"/> for configuring Cratis.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the full Cratis stack — Arc (commands and queries) together with the Chronicle event store client
    /// and Microsoft Identity Platform authentication — to the <see cref="WebApplicationBuilder"/>. This is the
    /// idiomatic, batteries-included setup for an event-sourced application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a composition: it is equivalent to calling <c>AddCratisArc</c> and then <c>WithChronicle</c> on
    /// the resulting Arc builder, plus <c>AddMicrosoftIdentityPlatformIdentityAuthentication</c>. Pair it with
    /// <c>UseCratis</c> on the built application, which activates both halves for you.
    /// </para>
    /// <para>
    /// What is added to your process is the Chronicle client, not the Chronicle engine. The client connects
    /// (over gRPC, using the connection string from configuration) to a Chronicle instance that runs on its own
    /// — typically the <c>cratis/chronicle</c> container. Nothing runs an event store inside your application,
    /// and the same code connects to a local container in development and a shared instance in production.
    /// </para>
    /// <para>
    /// To run Arc without an event store, call <c>AddCratisArc</c> on its own and back commands and queries with
    /// MongoDB or EF Core. To use Chronicle but supply your own authentication, call <c>AddCratisArc</c> and
    /// <c>WithChronicle</c> yourself instead of <c>AddCratis</c>.
    /// </para>
    /// </remarks>
    /// <param name="builder"><see cref="WebApplicationBuilder"/> to extend.</param>
    /// <param name="configureArcOptions">An optional action to configure <see cref="ArcOptions"/>.</param>
    /// <param name="configureArcBuilder">An optional action to configure the <see cref="ArcBuilder"/>.</param>
    /// <param name="configureChronicleOptions">An optional action to configure <see cref="ChronicleAspNetCoreOptions"/>.</param>
    /// <param name="configureChronicleBuilder">An optional action to configure the <see cref="ChronicleBuilder"/>.</param>
    /// <returns><see cref="WebApplicationBuilder"/> for building continuation.</returns>
    /// <example>
    /// The idiomatic setup — Arc, the Chronicle client, and identity in one call, paired with <c>UseCratis</c>:
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    ///
    /// builder.AddCratis();
    ///
    /// var app = builder.Build();
    ///
    /// app.UseCratis();
    /// app.Run();
    /// </code>
    /// </example>
    public static WebApplicationBuilder AddCratis(
        this WebApplicationBuilder builder,
        Action<ArcOptions>? configureArcOptions = default,
        Action<IArcBuilder>? configureArcBuilder = default,
        Action<ChronicleAspNetCoreOptions>? configureChronicleOptions = default,
        Action<IChronicleBuilder>? configureChronicleBuilder = default)
    {
        builder.AddCratisArc(
            configureOptions: configureArcOptions,
            configureBuilder: arcBuilder =>
            {
                configureArcBuilder?.Invoke(arcBuilder);
                arcBuilder.WithChronicle(configureChronicleOptions, configureChronicleBuilder);
            });

        builder.Services.AddMicrosoftIdentityPlatformIdentityAuthentication();

        return builder;
    }
}
