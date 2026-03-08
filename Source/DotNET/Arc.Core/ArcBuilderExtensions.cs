// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc;

/// <summary>
/// Extension methods for <see cref="IArcBuilder"/> for registering dependency resolvers.
/// </summary>
public static class ArcBuilderExtensions
{
    /// <summary>
    /// Registers a custom <see cref="ICommandDependencyResolver"/> with the Arc builder.
    /// </summary>
    /// <typeparam name="T">The type of the command dependency resolver to register.</typeparam>
    /// <param name="builder">The <see cref="IArcBuilder"/> to add to.</param>
    /// <returns><see cref="IArcBuilder"/> for continuation.</returns>
    public static IArcBuilder WithCommandDependencyResolver<T>(this IArcBuilder builder)
        where T : class, ICommandDependencyResolver
    {
        builder.Services.AddSingleton<ICommandDependencyResolver, T>();
        return builder;
    }

    /// <summary>
    /// Registers a custom <see cref="IQueryDependencyResolver"/> with the Arc builder.
    /// </summary>
    /// <typeparam name="T">The type of the query dependency resolver to register.</typeparam>
    /// <param name="builder">The <see cref="IArcBuilder"/> to add to.</param>
    /// <returns><see cref="IArcBuilder"/> for continuation.</returns>
    public static IArcBuilder WithQueryDependencyResolver<T>(this IArcBuilder builder)
        where T : class, IQueryDependencyResolver
    {
        builder.Services.AddSingleton<IQueryDependencyResolver, T>();
        return builder;
    }
}
