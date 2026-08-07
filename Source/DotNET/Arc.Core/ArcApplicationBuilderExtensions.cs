// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc;

/// <summary>
/// Extension methods for <see cref="ArcApplicationBuilder"/>.
/// </summary>
public static class ArcApplicationBuilderExtensions
{
    /// <summary>
    /// Adds Cratis Arc services — commands, queries, validation, tenancy, and proxy generation — to the
    /// <see cref="ArcApplicationBuilder"/>. On its own this wires Arc with no event store; chain
    /// <c>WithChronicle</c> via <paramref name="configureBuilder"/> to add event sourcing.
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

        builder.Services
            .AddOptions<ArcOptions>()
            .ValidateOnStart();

        if (configureOptions is not null)
        {
            builder.Services.PostConfigure(configureOptions);
        }

        if (configureBuilder is not null)
        {
            var arcBuilder = new ArcBuilder(builder, Internals.Types);
            configureBuilder.Invoke(arcBuilder);
        }

        builder.Services.AddSingleton<Http.IHttpRequestContextAccessor, Http.HttpRequestContextAccessor>();
        builder.Services.AddTransient<IObservableQueryHandler, ObservableQueryHandler>();
        builder.Services.AddIdentityProvider();
        builder.SkipEagerServiceProviderValidation();

        return builder;
    }

    /// <summary>
    /// Turns off eager service-provider validation without discarding the other options the host applied.
    /// </summary>
    /// <param name="builder">The <see cref="ArcApplicationBuilder"/> to configure.</param>
    /// <remarks>
    /// <para>
    /// Arc supplies registrations contextually — <see cref="IHostApplicationBuilder"/>, the type a convention
    /// binding is for, values only an executing command or an in-flight request can hand over. Eager validation
    /// constructs every registration up front and can resolve none of them, so an Arc application would fail
    /// <see cref="ArcApplicationBuilder.Build"/> outright in Development, where the host turns
    /// <see cref="ServiceProviderOptions.ValidateOnBuild"/> on.
    /// </para>
    /// <para>
    /// <see cref="ServiceProviderOptions.ValidateScopes"/> is restated from the environment so that turning eager
    /// validation off does not also take the host's Development-time captive-dependency detection with it.
    /// </para>
    /// </remarks>
    static void SkipEagerServiceProviderValidation(this ArcApplicationBuilder builder) =>
        builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions
        {
            ValidateScopes = builder.Environment.IsDevelopment(),
            ValidateOnBuild = false
        }));
}
