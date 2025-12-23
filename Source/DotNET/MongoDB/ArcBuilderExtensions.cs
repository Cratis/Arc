// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc;

/// <summary>
/// Provides extension methods for <see cref="IArcBuilder"/>.
/// </summary>
public static class ArcBuilderExtensions
{
    /// <summary>
    /// Add MongoDB to the solution. Configures default settings for the MongoDB Driver.
    /// </summary>
    /// <param name="arcBuilder"><see cref="IArcBuilder"/> to use MongoDB with.</param>
    /// <param name="configureOptions">Optional callback for configuring <see cref="MongoDBOptions"/>.</param>
    /// <param name="configureMongoDB">The optional callback for configuring <see cref="IMongoDBBuilder"/>.</param>
    /// <param name="mongoDBConfigSectionPath">Optional string for the <see cref="MongoDBOptions"/> config section path.</param>
    /// <returns><see cref="IArcBuilder"/> for building continuation.</returns>
    public static IArcBuilder WithMongoDB(
        this IArcBuilder arcBuilder,
        Action<MongoDBOptions>? configureOptions = default,
        Action<IMongoDBBuilder>? configureMongoDB = default,
        string? mongoDBConfigSectionPath = null)
    {
        arcBuilder.Services.AddCratisMongoDB(
            configureOptions: configureOptions,
            configureMongoDB: configureMongoDB,
            mongoDBConfigSectionPath: mongoDBConfigSectionPath);
        return arcBuilder;
    }
}
