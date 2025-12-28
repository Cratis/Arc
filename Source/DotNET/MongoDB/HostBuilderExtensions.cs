// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for <see cref="IHostBuilder"/>.
/// </summary>
public static class HostBuilderExtensions
{
    /// <summary>
    /// Add MongoDB to the solution. Configures default settings for the MongoDB Driver.
    /// </summary>
    /// <param name="builder"><see cref="IHostBuilder"/> to use MongoDB with.</param>
    /// <param name="configureOptions">Optional callback for configuring <see cref="MongoDBOptions"/>.</param>
    /// <param name="configureMongoDB">The optional callback for configuring <see cref="IMongoDBBuilder"/>.</param>
    /// <param name="mongoDBConfigSectionPath">Optional string for the <see cref="MongoDBOptions"/> config section path.</param>
    /// <returns><see cref="IHostBuilder"/> for building continuation.</returns>
    /// <remarks>
    /// It will automatically hook up any implementations of <see cref="IBsonClassMapFor{T}"/>
    /// and <see cref="ICanFilterMongoDBConventionPacksForType"/>.
    /// </remarks>
    public static IHostBuilder AddCratisMongoDB(
        this IHostBuilder builder,
        Action<MongoDBOptions>? configureOptions = default,
        Action<IMongoDBBuilder>? configureMongoDB = default,
        string? mongoDBConfigSectionPath = null) => builder.ConfigureServices((context, services) => services.AddCratisMongoDB(
            configureOptions,
            configureMongoDB,
            mongoDBConfigSectionPath));
}
