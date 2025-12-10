// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.Arc.MongoDB;

/// <summary>
/// Represents the setup of MongoDB defaults.
/// </summary>
public static class MongoDBDefaults
{
#if NET9_0
    static readonly Lock _lock = new();
#else
    static readonly object _lock = new();
#endif

    static bool _initialized;

    /// <summary>
    /// Initializes the MongoDB defaults.
    /// </summary>
    /// <param name="builder">An instance of a <see cref="IMongoDBBuilder"/> used for configuring the defaults.</param>
    public static void Initialize(IMongoDBBuilder builder)
    {
        lock (_lock)
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;

            var conventionPackFilters = builder
                .ConventionPackFilters
                .Select(_ => (Activator.CreateInstance(_) as ICanFilterMongoDBConventionPacksForType)!)
                .ToArray();

            RegisterConventionPacks(builder, conventionPackFilters);

            BsonSerializer
                .RegisterSerializationProvider(new ConceptSerializationProvider());
            BsonSerializer
                .RegisterSerializer(new DateTimeOffsetSupportingBsonDateTimeSerializer());
            BsonSerializer
                .RegisterSerializer(new DateOnlySerializer());
            BsonSerializer
                .RegisterSerializer(new TimeOnlySerializer());
            BsonSerializer
                .RegisterSerializer(new TimeSpanSerializer());
            BsonSerializer
                .RegisterSerializer(new TypeSerializer());
            BsonSerializer
                .RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

            // When you have types with properties defined as object but could hold a Guid, the GuidRepresentation gets by default set to Unspecified.
            // By adding an object serializer for object configured explicitly with the Standard representation it should get serialized correctly and not throw an exception.
            // As described here: https://jira.mongodb.org/browse/CSHARP-3780
            BsonSerializer
                .RegisterSerializer(new ObjectSerializer(CustomObjectDiscriminatorConvention.Instance, GuidRepresentation.Standard, t => true));

            RegisterConventionAsPack(conventionPackFilters, NamingPolicyNameConvention.ConventionName, new NamingPolicyNameConvention());
            RegisterConventionAsPack(conventionPackFilters, ConventionPacks.IgnoreExtraElements, new IgnoreExtraElementsConvention(true));

            RegisterClassMaps(builder);
        }
    }

    static void RegisterConventionPacks(IMongoDBBuilder builder, IEnumerable<ICanFilterMongoDBConventionPacksForType> conventionPackFilters)
    {
        foreach (var providerType in builder.ConventionPackProviders)
        {
            var provider = Activator.CreateInstance(providerType) as ICanProvideMongoDBConventionPacks;
            if (provider is not null)
            {
                foreach (var pack in provider.Provide())
                {
                    ConventionRegistry.Register(pack.Name, pack.ConventionPack, type => ShouldInclude(conventionPackFilters, pack.Name, pack.ConventionPack, type));
                }
            }
        }
    }

    static void RegisterClassMaps(IMongoDBBuilder builder)
    {
        foreach (var classMapType in builder.ClassMaps)
        {
            var classMapProvider = Activator.CreateInstance(classMapType);
            var typeInterfaces = classMapType.GetInterfaces().Where(_ =>
            {
                var args = _.GetGenericArguments();
                if (args.Length == 1)
                {
                    return _ == typeof(IBsonClassMapFor<>).MakeGenericType(args[0]);
                }
                return false;
            });

            var method = typeof(MongoDBDefaults).GetMethod(nameof(Register), BindingFlags.Static | BindingFlags.NonPublic)!;
            foreach (var type in typeInterfaces)
            {
                var genericMethod = method.MakeGenericMethod(type.GenericTypeArguments[0]);
                genericMethod.Invoke(null, [classMapProvider]);
            }
        }
    }

    static void Register<T>(IBsonClassMapFor<T> classMapProvider)
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(T)))
        {
            return;
        }

        var classMap = BsonClassMap.RegisterClassMap<T>(classMapProvider.Configure);
        classMap.ApplyConventions();
    }

    static void RegisterConventionAsPack(IEnumerable<ICanFilterMongoDBConventionPacksForType> conventionPackFilters, string name, IConvention convention)
    {
        var pack = new ConventionPack { convention };
        ConventionRegistry.Register(name, pack, type => ShouldInclude(conventionPackFilters, name, pack, type));
    }

    static bool ShouldInclude(IEnumerable<ICanFilterMongoDBConventionPacksForType> conventionPackFilters, string conventionPackName, IConventionPack conventionPack, Type type)
    {
        foreach (var filter in conventionPackFilters)
        {
            if (!filter.ShouldInclude(conventionPackName, conventionPack, type))
            {
                return false;
            }
        }

        return true;
    }
}
