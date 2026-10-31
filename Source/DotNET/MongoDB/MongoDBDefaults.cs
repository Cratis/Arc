// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
#if NET9_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif
using Cratis.Arc.Queries.ModelBound;
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
            BsonSerializer
                .RegisterSerializer(new PointSerializer());
            BsonSerializer
                .RegisterSerializer(new LineStringSerializer());
            BsonSerializer
                .RegisterSerializer(new PolygonSerializer());
            BsonSerializer
                .RegisterSerializer(new JsonObjectBsonSerializer());

            // When you have types with properties defined as object but could hold a Guid, the GuidRepresentation gets by default set to Unspecified.
            // By adding an object serializer for object configured explicitly with the Standard representation it should get serialized correctly and not throw an exception.
            // As described here: https://jira.mongodb.org/browse/CSHARP-3780
            BsonSerializer
                .RegisterSerializer(new ObjectSerializer(CustomObjectDiscriminatorConvention.Instance, GuidRepresentation.Standard, t => true));

            RegisterConventionAsPack(conventionPackFilters, NamingPolicyNameConvention.ConventionName, new NamingPolicyNameConvention());
            RegisterConventionAsPack(conventionPackFilters, ConventionPacks.IgnoreExtraElements, new IgnoreExtraElementsConvention(true));

            // Scoped to read models rather than every BSON type in the process: a collection that materializes as empty
            // instead of null is what a read model's own declaration already promises its callers, while for an
            // arbitrary document type it would be a change to what null means. Registered here, before RegisterClassMaps
            // below and before anything can deserialize, because a class map freezes its conventions the first time it
            // is built.
            RegisterConventionAsPack(
                conventionPackFilters,
                ConventionPacks.ReadModelCollectionsNeverNull,
                new ReadModelCollectionsNeverNullConvention(),
                type => type.IsReadModel());

            RegisterDerivedTypeDiscriminatorConventions();
            RegisterClassMaps(builder);
        }
    }

    /// <summary>
    /// Registers the class maps of an <see cref="IMongoDBBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IMongoDBBuilder"/> holding the class map providers.</param>
    internal static void RegisterClassMaps(IMongoDBBuilder builder)
    {
        foreach (var classMapType in builder.ClassMaps)
        {
            var classMapProvider = Activator.CreateInstance(classMapType);
            foreach (var type in classMapType.GetInterfaces().Where(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IBsonClassMapFor<>)))
            {
                RegisterClassMap(type.GenericTypeArguments[0], classMapProvider);
            }
        }
    }

#if NET9_0_OR_GREATER
    /// <summary>
    /// Registers a class map without closing a generic method through reflection over dynamic code.
    /// </summary>
    /// <param name="mappedType">The type the class map is for.</param>
    /// <param name="classMapProvider">The <see cref="IBsonClassMapFor{T}"/> provider for the type.</param>
    internal static void RegisterClassMapWithoutDynamicCode(Type mappedType, object? classMapProvider) =>
        typeof(MongoDBDefaults)
            .GetMethod(nameof(RegisterReferenceClassMap), BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(mappedType)
            .Invoke(null, [classMapProvider]);
#endif

    static void RegisterDerivedTypeDiscriminatorConventions()
    {
        var derivedTypes = Internals.DerivedTypesOrDefault;
        if (derivedTypes is null)
        {
            return;
        }

        var convention = new DerivedTypeDiscriminatorConvention(derivedTypes);
        foreach (var type in derivedTypes.TypesWithDerivatives)
        {
            BsonSerializer.RegisterDiscriminatorConvention(type, convention);
        }

        // The concrete derived types need the same convention on their own class maps — the driver resolves
        // the discriminator per class map when delegating from an interface serializer, and the default "_t"
        // convention answers with the interface nominal type, looping the delegation until the stack overflows.
        // A class map convention registers them lazily as each class map is created, independent of when the
        // derived types were discovered. Deliberately unfiltered: discriminator correctness is not optional.
        var derivedTypePack = new ConventionPack { new DerivedTypeClassMapConvention(derivedTypes, convention) };
        ConventionRegistry.Register("Derived type discriminator for derived types", derivedTypePack, _ => true);
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

    static void RegisterClassMap(Type mappedType, object? classMapProvider)
    {
#if NET9_0_OR_GREATER
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            RegisterClassMapWithoutDynamicCode(mappedType, classMapProvider);
            return;
        }
#endif

        RegisterClassMapThroughReflection(mappedType, classMapProvider);
    }

    [RequiresDynamicCode("Closes the class map registration over a type only known at runtime.")]
    static void RegisterClassMapThroughReflection(Type mappedType, object? classMapProvider)
    {
        var method = typeof(MongoDBDefaults).GetMethod(nameof(Register), BindingFlags.Static | BindingFlags.NonPublic)!;
        var genericMethod = method.MakeGenericMethod(mappedType);
        genericMethod.Invoke(null, [classMapProvider]);
    }

#if NET9_0_OR_GREATER
    static void RegisterReferenceClassMap<T>(object? classMapProvider)
        where T : class => Register((IBsonClassMapFor<T>)classMapProvider!);
#endif

    static void Register<T>(IBsonClassMapFor<T> classMapProvider)
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(T)))
        {
            return;
        }

        var classMap = BsonClassMap.RegisterClassMap<T>(classMapProvider.Configure);
        classMap.ApplyConventions();
    }

    static void RegisterConventionAsPack(IEnumerable<ICanFilterMongoDBConventionPacksForType> conventionPackFilters, string name, IConvention convention, Func<Type, bool>? appliesTo = default)
    {
        var pack = new ConventionPack { convention };
        ConventionRegistry.Register(
            name,
            pack,
            type => (appliesTo?.Invoke(type) ?? true) && ShouldInclude(conventionPackFilters, name, pack, type));
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
