// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IMongoDBBuilder"/>.
/// </summary>
public class MongoDBBuilder : IMongoDBBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBBuilder"/> class.
    /// </summary>
    public MongoDBBuilder()
    {
        var types = TypesServiceCollectionExtensions.CurrentTypeUniverse();
        ClassMaps = [.. types.FindMultiple(typeof(IBsonClassMapFor<>))];
        ConventionPackFilters = [.. types.FindMultiple<ICanFilterMongoDBConventionPacksForType>()];
        ConventionPackProviders = [.. types.FindMultiple<ICanProvideMongoDBConventionPacks>()];
    }

    /// <inheritdoc/>
    public IList<Type> ClassMaps { get; }

    /// <inheritdoc/>
    public IList<Type> ConventionPackFilters { get; }

    /// <inheritdoc/>
    public IList<Type> ConventionPackProviders { get; }

    /// <inheritdoc/>
    public Type ServerResolverType { get; set; } = typeof(DefaultMongoServerResolver);

    /// <inheritdoc/>
    public Type DatabaseNameResolverType { get; set; } = typeof(DefaultMongoDatabaseNameResolver);

    /// <inheritdoc/>
    public INamingPolicy? NamingPolicy { get; set; } = new DefaultNamingPolicy();

    /// <inheritdoc/>
    public void Validate()
    {
        MongoServerResolverNotConfigured.ThrowIfNotConfigured(ServerResolverType);
        MongoDatabaseNameResolverNotConfigured.ThrowIfNotConfigured(DatabaseNameResolverType);
        NamingPolicyNotConfigured.ThrowIfNotConfigured(NamingPolicy);
    }
}
