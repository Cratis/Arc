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
    /// Adds Cratis Arc — commands, queries, validation, tenancy, and proxy generation — to a generic
    /// <see cref="IHostBuilder"/>. On its own this wires Arc with no event store.
    /// </summary>
    /// <remarks>
    /// Binds the <see cref="ArcOptions"/> configuration to the given config section path or the default
    /// Cratis:Arc section path. To add event sourcing, chain <c>WithChronicle</c> via
    /// <paramref name="configureBuilder"/>, or use <c>AddCratis</c> for the all-in-one setup.
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

    /// <summary>
    /// Turns off eager service-provider validation without discarding the other options the host applied.
    /// </summary>
    /// <param name="builder"><see cref="IHostBuilder"/> to extend.</param>
    /// <returns><see cref="IHostBuilder"/> for building continuation.</returns>
    /// <remarks>
    /// <para>
    /// Arc supplies registrations contextually — <see cref="IHostApplicationBuilder"/>, the type a convention
    /// binding is for, values only an executing command or an in-flight HTTP request can hand over. Eager
    /// validation constructs every registration up front and can resolve none of them, so
    /// <see cref="ServiceProviderOptions.ValidateOnBuild"/> has to be off.
    /// </para>
    /// <para>
    /// Both <c>UseDefaultServiceProvider</c> overloads hand their callback a brand new
    /// <see cref="ServiceProviderOptions"/>, so setting a single field discards every other value the host had
    /// applied — including <see cref="ServiceProviderOptions.ValidateScopes"/>, which the host derives from
    /// <c>IsDevelopment()</c>. Restating it keeps the framework's Development-time captive-dependency detection on
    /// for applications that add Arc. An application wanting a different value states it by calling
    /// <c>UseDefaultServiceProvider</c> itself after Arc has been added.
    /// </para>
    /// </remarks>
    internal static IHostBuilder SkipEagerServiceProviderValidation(this IHostBuilder builder) =>
        builder.UseDefaultServiceProvider((context, options) =>
        {
            options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
            options.ValidateOnBuild = false;
        });

    static IHostBuilder AddArcImplementation(this IHostBuilder builder)
    {
        builder.SkipEagerServiceProviderValidation();
        builder.AddCorrelationIdLogEnricher();

        builder
            .ConfigureServices(services =>
            {
                services.AddHttpContextAccessor();
                services.AddControllersFromProjectReferencedAssembles(Internals.Types);
            });

        return builder;
    }
}
