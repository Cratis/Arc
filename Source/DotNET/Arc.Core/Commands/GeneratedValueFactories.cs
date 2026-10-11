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
    static readonly ConcurrentDictionary<Type, Func<object>> _factories = new();
    static readonly ConcurrentDictionary<Type, Func<object>> _reflectionFactories = new();

    /// <summary>
    /// Registers a factory emitted by the Arc source generator.
    /// </summary>
    /// <param name="type">The exact generated value type.</param>
    /// <param name="factory">The factory that creates that type.</param>
    public static void Register(Type type, Func<object> factory) => _factories[type] = factory;

    /// <summary>
    /// Creates a value through its static interface contract, including explicit implementations.
    /// </summary>
    /// <typeparam name="T">The generatable value type.</typeparam>
    /// <returns>A new value.</returns>
    public static T New<T>()
        where T : IGeneratable<T> => T.New();

    /// <summary>
    /// Determines whether a typed factory is registered.
    /// </summary>
    /// <param name="type">The generated value type.</param>
    /// <returns>Whether the registry has a factory for this type.</returns>
    internal static bool HasFactory(Type type) => _factories.ContainsKey(type);

    /// <summary>
    /// Creates a value using a registered factory or the guarded reflection fallback.
    /// </summary>
    /// <param name="commandType">The command declaring the parameter.</param>
    /// <param name="parameter">The generated parameter.</param>
    /// <returns>The generated value.</returns>
    /// <exception cref="MissingGeneratedValueFactory">No factory is registered and dynamic code is unavailable.</exception>
    internal static object Create(Type commandType, ParameterInfo parameter)
    {
        var type = parameter.ParameterType;
        if (_factories.TryGetValue(type, out var factory))
        {
            return factory();
        }

        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new MissingGeneratedValueFactory(commandType, parameter.Name!, type);
        }

        return _reflectionFactories.GetOrAdd(type, CreateThroughReflection)();
    }

    [RequiresDynamicCode("Creates a constrained generated value factory for a type only known at runtime.")]
    static Func<object> CreateThroughReflection(Type type) =>
        typeof(GeneratedValueFactories).GetMethod(nameof(CreateValue), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(type).CreateDelegate<Func<object>>();

    static object CreateValue<T>()
        where T : IGeneratable<T> => New<T>();
}
