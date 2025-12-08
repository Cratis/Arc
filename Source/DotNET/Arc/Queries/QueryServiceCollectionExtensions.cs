// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
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
        services.AddSingleton<IQueryPerformerProviders, QueryPerformerProviders>();
        services.AddSingleton<IQueryRenderers, QueryRenderers>();
        return services;
    }
}
