// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Cratis.Json;
using Cratis.Serialization;

namespace Cratis.Arc;

/// <summary>
/// Extension methods for configuring <see cref="JsonSerializerOptions"/>.
/// </summary>
public static class JsonSerializerOptionsConfiguration
{
    /// <summary>
    /// Gets the reflection-based resolver <see cref="JsonSerializer"/> itself falls back to, or <see langword="null"/> when
    /// reflection-based serialization is disabled.
    /// </summary>
    /// <remarks>
    /// <see cref="JsonSerializerOptions.Default"/> carries that resolver only while <see cref="JsonSerializer.IsReflectionEnabledByDefault"/>
    /// holds, and an empty resolver otherwise; the feature switch lets the trimmer remove the reflection path.
    /// </remarks>
    internal static IJsonTypeInfoResolver? ReflectionResolver =>
        JsonSerializer.IsReflectionEnabledByDefault ? JsonSerializerOptions.Default.TypeInfoResolver : null;

    /// <summary>
    /// Configure the <see cref="JsonSerializerOptions"/> with Arc defaults.
    /// </summary>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to configure.</param>
    /// <param name="derivedTypes">Optional <see cref="IDerivedTypes"/> to use for derived type serialization.</param>
    /// <returns>The configured <see cref="JsonSerializerOptions"/> for continuation.</returns>
    /// <remarks>
    /// <para>
    /// Besides naming, number handling and converters, this sets up how the options resolve type metadata. Arc adds a single
    /// resolver to the end of <see cref="JsonSerializerOptions.TypeInfoResolverChain"/>, after any resolver the options were
    /// already configured with, so the application's own contracts win. That resolver consults, in order:
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// Any resolver appended to the chain after this call, as <c>TypeInfoResolverChain.Add(...)</c> made it the only resolver
    /// before Arc shipped source-generated metadata.
    /// </description></item>
    /// <item><description>Arc's source-generated metadata for its own wire types, such as the command and query results.</description></item>
    /// <item><description>
    /// The reflection-based resolver, only when reflection-based serialization is enabled
    /// (<see cref="JsonSerializer.IsReflectionEnabledByDefault"/>). It is the resolver <see cref="JsonSerializer"/> falls back
    /// to for options that have none, so every type that serialized before still serializes the same way.
    /// </description></item>
    /// </list>
    /// <para>
    /// Add an application's source-generated <see cref="JsonSerializerContext"/> through
    /// <see cref="ArcOptions.AddJsonTypeInfoResolver"/>, which places it ahead of Arc's resolver. Assigning
    /// <see cref="JsonSerializerOptions.TypeInfoResolver"/> replaces the whole chain, Arc's resolver included.
    /// </para>
    /// </remarks>
    public static JsonSerializerOptions ConfigureArcDefaults(this JsonSerializerOptions options, IDerivedTypes? derivedTypes = null)
    {
        options.PropertyNamingPolicy = AcronymFriendlyJsonCamelCaseNamingPolicy.Instance;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;

        options.Converters.Add(new ComplexKeyDictionaryJsonConverterFactory());
        options.Converters.Add(new EnumConverterFactory());
        options.Converters.Add(new EnumerableConceptAsJsonConverterFactory());
        options.Converters.Add(new ConceptAsJsonConverterFactory());
        options.Converters.Add(new DateOnlyJsonConverter());
        options.Converters.Add(new TimeOnlyJsonConverter());
        options.Converters.Add(new TypeJsonConverter());
        options.Converters.Add(new UriJsonConverter());
        options.Converters.Add(new PointJsonConverter());
        options.Converters.Add(new LineStringJsonConverter());
        options.Converters.Add(new PolygonJsonConverter());
        options.Converters.Add(new EnumerableModelWithIdToConceptOrPrimitiveEnumerableConverterFactory());

        if (derivedTypes is not null)
        {
            options.Converters.Add(new DerivedTypeJsonConverterFactory(derivedTypes));
        }

        ComposeTypeInfoResolvers(options);

        return options;
    }

    /// <summary>
    /// Add a type info resolver ahead of Arc's resolver, and with it Arc's own metadata and the reflection-based fallback.
    /// </summary>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> configured with <see cref="ConfigureArcDefaults"/>.</param>
    /// <param name="resolver">The <see cref="IJsonTypeInfoResolver"/> to add.</param>
    /// <remarks>
    /// Resolvers added this way are consulted in the order they were added. A resolver already in the chain is not added
    /// again. When the options no longer hold Arc's resolver - because the application replaced
    /// <see cref="JsonSerializerOptions.TypeInfoResolver"/> - the resolver goes last.
    /// </remarks>
    internal static void AddTypeInfoResolverBeforeArcDefaults(this JsonSerializerOptions options, IJsonTypeInfoResolver resolver)
    {
        var resolvers = options.TypeInfoResolverChain.ToList();
        if (resolvers.Contains(resolver))
        {
            return;
        }

        var index = IndexOfArcResolver(resolvers);
        resolvers.Insert(index < 0 ? resolvers.Count : index, resolver);
        AssignResolvers(options, resolvers);
    }

    /// <summary>
    /// Get the <see cref="JsonTypeInfo"/> for a type the way <see cref="JsonSerializer"/> resolves it for these options.
    /// </summary>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to resolve through.</param>
    /// <param name="type">The <see cref="Type"/> to get the <see cref="JsonTypeInfo"/> for.</param>
    /// <returns>The <see cref="JsonTypeInfo"/>.</returns>
    /// <remarks>
    /// Serializing through the returned <see cref="JsonTypeInfo"/> gives the same JSON as passing the options and the
    /// type to <see cref="JsonSerializer"/>. Options configured with <see cref="ConfigureArcDefaults"/> always carry a
    /// resolver; for options that carry none, the options are made read-only with the reflection-based resolver the
    /// serializer itself falls back to filled in, exactly as the serializer does on first use.
    /// </remarks>
    /// <exception cref="NotSupportedException">No resolver in the chain knows the type.</exception>
    internal static JsonTypeInfo ResolveTypeInfo(this JsonSerializerOptions options, Type type)
    {
        if (options.TypeInfoResolver is null && JsonSerializer.IsReflectionEnabledByDefault)
        {
            // The serializer's own path: idempotent and safe when another thread makes the options read-only first,
            // unlike checking IsReadOnly and assigning TypeInfoResolver.
            options.MakeReadOnly(populateMissingResolver: true);
        }

        return options.GetTypeInfo(type);
    }

    /// <summary>
    /// Get the <see cref="JsonTypeInfo{T}"/> for a type the way <see cref="JsonSerializer"/> resolves it for these options.
    /// </summary>
    /// <typeparam name="T">The type to get the <see cref="JsonTypeInfo{T}"/> for.</typeparam>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to resolve through.</param>
    /// <returns>The <see cref="JsonTypeInfo{T}"/>.</returns>
    /// <remarks>See <see cref="ResolveTypeInfo(JsonSerializerOptions, Type)"/>.</remarks>
    /// <exception cref="NotSupportedException">No resolver in the chain knows the type.</exception>
    internal static JsonTypeInfo<T> ResolveTypeInfo<T>(this JsonSerializerOptions options) =>
        (JsonTypeInfo<T>)options.ResolveTypeInfo(typeof(T));

    static void ComposeTypeInfoResolvers(JsonSerializerOptions options)
    {
        var resolvers = options.TypeInfoResolverChain.ToList();
        if (IndexOfArcResolver(resolvers) < 0)
        {
            resolvers.Add(new ArcDefaultsJsonTypeInfoResolver(options, ReflectionResolver));
            AssignResolvers(options, resolvers);
        }
    }

    /// <summary>
    /// Assign the resolvers as a standalone chain, leaving the chain bound to the options untouched.
    /// </summary>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to assign to.</param>
    /// <param name="resolvers">The resolvers, in the order they are consulted.</param>
    /// <remarks>
    /// On .NET 8 and .NET 9, modifying <see cref="JsonSerializerOptions.TypeInfoResolverChain"/> makes that chain object
    /// the options' <see cref="JsonSerializerOptions.TypeInfoResolver"/>, and assigning the property clears and refills the
    /// same object. The common <c>options.TypeInfoResolver = options.TypeInfoResolver.WithAddedModifier(...)</c> would then
    /// leave a chain holding a wrapper around itself, recursing until the process dies on first use. Assigning a chain of
    /// its own keeps the wrapped resolver apart from the chain the property refills.
    /// </remarks>
    static void AssignResolvers(JsonSerializerOptions options, List<IJsonTypeInfoResolver> resolvers) =>
        options.TypeInfoResolver = JsonTypeInfoResolver.Combine([.. resolvers]);

    static int IndexOfArcResolver(List<IJsonTypeInfoResolver> chain)
    {
        for (var i = 0; i < chain.Count; i++)
        {
            if (chain[i] is ArcDefaultsJsonTypeInfoResolver)
            {
                return i;
            }
        }

        return -1;
    }
}
