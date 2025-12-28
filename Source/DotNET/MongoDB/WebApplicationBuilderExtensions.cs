// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides extension methods for the <see cref="WebApplicationBuilder"/> for configuring MongoDB.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Use MongoDB in the solution. Configures default settings for the MongoDB Driver.
    /// </summary>
    /// <param name="builder"><see cref="WebApplicationBuilder"/> to use MongoDB with.</param>
    /// <param name="configureOptions">Optional callback for configuring <see cref="MongoDBOptions"/>.</param>
    /// <param name="configureMongoDB">The optional callback for configuring <see cref="IMongoDBBuilder"/>.</param>
    /// <param name="mongoDBConfigSectionPath">Optional string for the <see cref="MongoDBOptions"/> config section path.</param>
    /// <returns><see cref="WebApplicationBuilder"/> for building continuation.</returns>
    /// <remarks>
    /// It will automatically hook up any implementations of <see cref="IBsonClassMapFor{T}"/>
    /// and <see cref="ICanFilterMongoDBConventionPacksForType"/>.
    /// </remarks>
    public static IHostBuilder UseCratisMongoDB(
        this WebApplicationBuilder builder,
        Action<MongoDBOptions>? configureOptions = default,
        Action<IMongoDBBuilder>? configureMongoDB = default,
        string? mongoDBConfigSectionPath = null) => builder.Host.AddCratisMongoDB(
            configureOptions,
            configureMongoDB,
            mongoDBConfigSectionPath);
}