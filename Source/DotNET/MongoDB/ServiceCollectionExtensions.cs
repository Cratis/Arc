// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Arc;
using Cratis.Arc.MongoDB;
using Cratis.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> for configuring MongoDB.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Default configuration section paths for Arc options.
    /// </summary>
    public static readonly string[] DefaultSectionPaths = ["Cratis", "MongoDB"];

    static IMongoDBClientFactory? _clientFactory;
    static IMongoServerResolver? _serverResolver;

    /// <summary>
    /// Add MongoDB to the solution. Configures default settings for the MongoDB Driver.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to use MongoDB with.</param>
    /// <param name="configureOptions">Optional callback for configuring <see cref="MongoDBOptions"/>.</param>
    /// <param name="configureMongoDB">The optional callback for configuring <see cref="IMongoDBBuilder"/>.</param>
    /// <param name="mongoDBConfigSectionPath">Optional string for the <see cref="MongoDBOptions"/> config section path.</param>
    /// <returns><see cref="IServiceCollection"/> for building continuation.</returns>
    public static IServiceCollection AddCratisMongoDB(
        this IServiceCollection services,
        Action<MongoDBOptions>? configureOptions = default,
        Action<IMongoDBBuilder>? configureMongoDB = default,
        string? mongoDBConfigSectionPath = null)
    {
        var mongoDBBuilder = CreateMongoDBBuilder(configureMongoDB);

        ConfigureNamingPolicy(mongoDBBuilder);
        MongoDBDefaults.Initialize(mongoDBBuilder);
        AddServices(
            services,
            mongoDBBuilder,
            mongoDBConfigSectionPath ?? ConfigurationPath.Combine(DefaultSectionPaths),
            configureOptions);
        return services;
    }

    static void ConfigureNamingPolicy(MongoDBBuilder mongoDBBuilder)
    {
        if (mongoDBBuilder.NamingPolicy is not null)
        {
            DatabaseExtensions.SetNamingPolicy(mongoDBBuilder.NamingPolicy);
        }
    }

    static void AddServices(
        IServiceCollection services,
        MongoDBBuilder mongoDBBuilder,
        string? mongoDBConfigSectionPath = null,
        Action<MongoDBOptions>? configureOptions = default)
    {
        var optionsBuilder = AddOptions(services, configureOptions);
        if (!string.IsNullOrWhiteSpace(mongoDBConfigSectionPath))
        {
            optionsBuilder.BindConfiguration(mongoDBConfigSectionPath);
        }

        services.AddKeyedSingleton<IMeter<IMongoClient>>(Internals.MeterName, (sp, key) =>
        {
            var meter = sp.GetRequiredKeyedService<Meter>(key);
            return new Meter<IMongoClient>(meter);
        });

        if (mongoDBBuilder.NamingPolicy is not null)
        {
            services.AddSingleton(mongoDBBuilder.NamingPolicy);
        }

        services.AddSingleton(typeof(IMongoServerResolver), mongoDBBuilder.ServerResolverType);
        services.AddSingleton(typeof(IMongoDatabaseNameResolver), mongoDBBuilder.DatabaseNameResolverType);
        services.AddSingleton<IMongoDBClientFactory, MongoDBClientFactory>();
        services.AddScoped(sp =>
        {
            // TODO: This will not work when multi tenant.
            _clientFactory ??= sp.GetRequiredService<IMongoDBClientFactory>();
            _serverResolver ??= sp.GetRequiredService<IMongoServerResolver>();
            return _clientFactory.Create();
        });

        services.AddScoped(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            var databaseNameResolver = sp.GetRequiredService<IMongoDatabaseNameResolver>();
            return client.GetDatabase(databaseNameResolver.Resolve());
        });

        services.AddScoped(typeof(IMongoCollection<>), typeof(MongoCollectionAdapter<>));
    }

    static MongoDBBuilder CreateMongoDBBuilder(Action<MongoDBBuilder>? configure)
    {
        var builder = new MongoDBBuilder();
        configure?.Invoke(builder);
        builder.Validate();
        return builder;
    }

    static OptionsBuilder<MongoDBOptions> AddOptions(IServiceCollection services, Action<MongoDBOptions>? configureOptions = default)
    {
        var builder = services
            .AddOptions<MongoDBOptions>()
            .ValidateOnStart()
            .ValidateDataAnnotations();

        if (configureOptions is not null)
        {
            builder.Configure(configureOptions);
        }

        return builder;
    }

}
