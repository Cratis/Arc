// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Cratis.Arc.Availability.Audiences;
using Cratis.Arc.Contexts;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding Arc availability gates to a service collection.
/// </summary>
public static class ArcAvailabilityServiceCollectionExtensions
{
    /// <summary>
    /// Adds the availability engine, with nothing that depends on a host: the evaluator, the audience resolver and defaults for
    /// the audience source, the declared audiences and the catalogs. A registration made before this call is kept.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">Optional configuration of <see cref="AvailabilityOptions"/>, such as the environment name.</param>
    /// <returns>The <see cref="IServiceCollection"/> for continuation.</returns>
    /// <remarks>
    /// The audience source defaults to <see cref="InMemoryAudienceSource"/>. A host that keeps audiences elsewhere registers its own
    /// <see cref="IAudienceSource"/> before or after this call.
    /// </remarks>
    public static IServiceCollection AddAvailability(this IServiceCollection services, Action<AvailabilityOptions>? configure = null)
    {
        var options = new AvailabilityOptions();
        configure?.Invoke(options);
        options.Validate();

        // The options are replaced, not added only when missing: a convention or self binding may already hold the type,
        // and the configured instance is the one that must be resolved.
        services.Replace(ServiceDescriptor.Singleton(options));
        services.TryAddSingleton<IAudienceSource, InMemoryAudienceSource>();
        services.TryAddSingleton<IDeclaredAudiences>(_ => new DeclaredAudiences(TypedAvailability.Audiences(_.GetService<ITypes>())));
        services.TryAddSingleton(TypedAvailability.CreateCatalog);
        services.TryAddSingleton(TypedAvailability.CreateContextValues);
        services.TryAddSingleton<IAudienceResolver, AudienceResolver>();
        services.TryAddSingleton<IAvailabilityEvaluator, AvailabilityEvaluator>();
        return services;
    }

    /// <summary>
    /// Adds availability gates to the command and query pipelines, the client availability query and MCP tool listing,
    /// and the engine they decide with.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">Optional configuration of gates that are not declared with attributes.</param>
    /// <param name="configureEngine">Optional configuration of the engine <see cref="AvailabilityOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for continuation.</returns>
    /// <remarks>
    /// The configured options are the ones resolved, whether or not Arc was added before. At startup the declared blocks are
    /// validated and the gated parts discovered once. A host that does not call this method pays nothing.
    /// </remarks>
    public static IServiceCollection AddArcAvailability(
        this IServiceCollection services,
        Action<ArcAvailabilityOptions>? configure = null,
        Action<AvailabilityOptions>? configureEngine = null)
    {
        var options = new ArcAvailabilityOptions();
        configure?.Invoke(options);

        services.AddAvailability(configureEngine);
        services.Replace(ServiceDescriptor.Singleton(options));
        services.Replace(ServiceDescriptor.Scoped<AvailabilityRequestContext, AvailabilityRequestContext>());
        services.TryAddSingleton<IAvailabilityGate, ArcAvailabilityGate>();
        services.TryAddScoped<IAvailabilitySubjectProvider, AvailabilitySubjectProvider>();
        services.TryAddScoped<IContextValueSource, AvailabilityContextValueSource>();
        services.TryAddScoped<IUnavailableParts, UnavailableParts>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, AvailabilityStartupValidation>());
        return services;
    }
}
