// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j;
using Cratis.Arc.Neo4j.Testing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Neo4j.Driver;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> for substituting the graph session in specifications and scenarios.
/// </summary>
public static class GraphSessionServiceCollectionExtensions
{
    /// <summary>
    /// Replace the graph session with a given <see cref="IGraphSession"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>, for instance a command scenario's <c>Services</c>.</param>
    /// <param name="session">The <see cref="IGraphSession"/> to give to everything that asks for a session or opens one through <see cref="IGraphSessionFactory"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection UseGraphSession(this IServiceCollection services, IGraphSession session)
    {
        services.RemoveAll<GraphConnection>();
        services.RemoveAll<IGraphSessionFactory>();
        services.RemoveAll<IGraphSession>();
        services.AddSingleton<IGraphSessionFactory>(new SubstituteGraphSessionFactory(session));
        services.AddSingleton(session);
        return services;
    }

    /// <summary>
    /// Replace the graph session with a new <see cref="GraphSessionSubstitute"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="GraphSessionSubstitute"/> to set up and inspect.</returns>
    public static GraphSessionSubstitute UseGraphSessionSubstitute(this IServiceCollection services)
    {
        var substitute = new GraphSessionSubstitute();
        services.UseGraphSession(substitute);
        return substitute;
    }

    sealed class SubstituteGraphSessionFactory(IGraphSession session) : IGraphSessionFactory
    {
        public IGraphSession Open(AccessMode accessMode = AccessMode.Read) => session;
    }
}
