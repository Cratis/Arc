// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extensions for adding Cratis query handling services to a service collection.
/// </summary>
public static class QueryServiceCollectionExtensions
{
    /// <summary>
    /// Adds Cratis query handling services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCratisQueries(this IServiceCollection services)
    {
        services.AddSingleton<IQueryContextManager, QueryContextManager>();
        services.AddSingleton<IQueryPipeline, QueryPipeline>();
        services.AddSingleton<IQueryFilters, QueryFilters>();
        services.AddSingleton<IQueryMetadataRegistry, QueryMetadataRegistry>();
        services.AddSingleton<Func<IServiceProvider, IAuthorizationEvaluator>>(_ =>
            scope => scope.GetRequiredService<IAuthorizationEvaluator>());
        services.AddSingleton<IQueryPerformerProviders, QueryPerformerProviders>();
        services.AddSingleton<QueryPerformerProvider>(sp => new QueryPerformerProvider(
            sp.GetRequiredService<ITypes>(),
            sp.GetRequiredService<IQueryMetadataRegistry>(),
            sp.GetRequiredService<IServiceProviderIsService>(),
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<Func<IServiceProvider, IAuthorizationEvaluator>>()));
        var modelBoundProvider = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IQueryPerformerProvider) &&
            descriptor.ImplementationType == typeof(QueryPerformerProvider));
        if (modelBoundProvider is not null)
        {
            var index = services.IndexOf(modelBoundProvider);
            services[index] = ServiceDescriptor.Singleton<IQueryPerformerProvider>(sp => sp.GetRequiredService<QueryPerformerProvider>());
        }
        services.AddSingleton<IQueryRenderers, QueryRenderers>();
        services.AddSingleton<IReadModelInterceptors, ReadModelInterceptors>();
        services.AddSingleton<IQueryHealthTracker, QueryHealthTracker>();

        return services;
    }
}
