// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Cratis.Arc.Commands;

/// <summary>
/// Registers the typed generated value factories emitted by the Arc source generator.
/// </summary>
/// <remarks>
/// The registered path does not require dynamic generic instantiation. This registry does not imply
/// that the rest of Arc supports NativeAOT.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class GeneratedValueFactories
{
    static readonly ConcurrentDictionary<Type, Func<GenerationContext, object>> _factories = new();
    static readonly ConcurrentDictionary<Type, Func<GenerationContext, object>> _contextFactories = new();
    static readonly ConcurrentDictionary<Type, Func<GenerationContext, object>> _reflectionFactories = new();

    /// <summary>
    /// Registers a factory emitted by the Arc source generator.
    /// </summary>
    /// <param name="type">The exact generated value type.</param>
    /// <param name="factory">The factory that creates that type.</param>
    public static void Register(Type type, Func<object> factory) => _factories[type] = _ => factory();

    /// <summary>
    /// Registers a context-dependent factory emitted by the Arc source generator.
    /// </summary>
    /// <param name="type">The exact generated value type.</param>
    /// <param name="factory">The factory that creates that type using the command's captured context.</param>
    public static void Register(Type type, Func<GenerationContext, object> factory) => _contextFactories[type] = factory;

    /// <summary>
    /// Creates a value through its static interface contract, including explicit implementations.
    /// </summary>
    /// <typeparam name="T">The generatable value type.</typeparam>
    /// <returns>A new value.</returns>
    public static T New<T>()
        where T : IGeneratable<T> => T.New();

    /// <summary>
    /// Creates a value through its context-dependent static interface contract.
    /// </summary>
    /// <typeparam name="T">The context-dependent generatable value type.</typeparam>
    /// <param name="context">The command's captured time and portable tenant identifier.</param>
    /// <returns>A new value.</returns>
    /// <exception cref="MissingGenerationTime">The context has no captured time.</exception>
    public static T New<T>(GenerationContext context)
        where T : IGeneratableInContext<T>
    {
        if (context.Now == default)
        {
            throw new MissingGenerationTime();
        }

        return T.New(context);
    }

    /// <summary>
    /// Determines whether a typed factory is registered.
    /// </summary>
    /// <param name="type">The generated value type.</param>
    /// <returns>Whether the registry has a factory for this type.</returns>
    internal static bool HasFactory(Type type) => FactoriesFor(type).ContainsKey(type);

    /// <summary>
    /// Creates a value using a registered factory or the guarded reflection fallback.
    /// </summary>
    /// <param name="commandType">The command declaring the parameter.</param>
    /// <param name="parameter">The generated parameter.</param>
    /// <param name="context">The command's generation context, used by context-dependent types.</param>
    /// <returns>The generated value.</returns>
    /// <exception cref="MissingGenerationTime">A context-dependent value has no captured time.</exception>
    /// <exception cref="MissingGeneratedValueFactory">No factory is registered and dynamic code is unavailable.</exception>
    internal static object Create(Type commandType, ParameterInfo parameter, GenerationContext context = default)
    {
        var type = parameter.ParameterType;
        if (GeneratedValueParameters.IsGeneratableInContext(type) && context.Now == default)
        {
            throw new MissingGenerationTime();
        }

        if (FactoriesFor(type).TryGetValue(type, out var factory))
        {
            return factory(context);
        }

        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new MissingGeneratedValueFactory(commandType, parameter.Name!, type);
        }

        return _reflectionFactories.GetOrAdd(type, CreateThroughReflection)(context);
    }

    static ConcurrentDictionary<Type, Func<GenerationContext, object>> FactoriesFor(Type type) =>
        GeneratedValueParameters.IsGeneratableInContext(type) ? _contextFactories : _factories;

    [RequiresDynamicCode("Creates a constrained generated value factory for a type only known at runtime.")]
    static Func<GenerationContext, object> CreateThroughReflection(Type type) =>
        typeof(GeneratedValueFactories).GetMethod(
            GeneratedValueParameters.IsGeneratableInContext(type) ? nameof(CreateValueInContext) : nameof(CreateValue),
            BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(type).CreateDelegate<Func<GenerationContext, object>>();

    static object CreateValue<T>(GenerationContext context)
        where T : IGeneratable<T>
    {
        _ = context;

        return New<T>();
    }

    static object CreateValueInContext<T>(GenerationContext context)
        where T : IGeneratableInContext<T> => New<T>(context);
}
