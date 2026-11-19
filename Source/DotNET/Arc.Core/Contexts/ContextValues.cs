// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Contexts;

/// <summary>
/// Represents the values a model adds to <c>$context</c> with <c>provide</c>, readable by name from the context of a command, query, policy or validation rule.
/// </summary>
/// <remarks>
/// <para>
/// A value resolves on its first read and is reused for the rest of the request, so every reader in the request sees one consistent value.
/// An absent value reads as <see langword="null"/>, which is unknown: a gate that reads it denies. A value declared as required throws
/// <see cref="RequiredContextValueAbsentException"/> when it is absent, and a failure to resolve throws <see cref="ContextValueUnavailableException"/>.
/// </para>
/// <para>
/// Reading by name, such as <c>context.Values.Vessel</c>, is synchronous. Prefer <see cref="GetAsync(string)"/> where the caller is asynchronous.
/// </para>
/// </remarks>
public sealed class ContextValues
{
    readonly IContextValueSource? _source;
    readonly Func<Type?> _part;
    readonly CancellationToken _cancellationToken;

    ContextValues(IContextValueSource? source, Func<Type?> part, CancellationToken cancellationToken)
    {
        _source = source;
        _part = part;
        _cancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the values of a request that provides none. Every value reads as absent.
    /// </summary>
    public static ContextValues Empty { get; } = new(null, () => null, CancellationToken.None);

    /// <summary>
    /// Gets a value indicating whether the host provides context values at all.
    /// </summary>
    public bool IsProvided => _source is not null;

    /// <summary>
    /// Creates the values of a request.
    /// </summary>
    /// <param name="services">The <see cref="IServiceProvider"/> scoped to the request; <see langword="null"/> when there is none.</param>
    /// <param name="part">Gets the part the values are read for, which decides the level whose declaration wins. It runs on the first read.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> of the request.</param>
    /// <returns>The <see cref="ContextValues"/>; <see cref="Empty"/> when the host provides no <see cref="IContextValueSource"/>.</returns>
    public static ContextValues For(IServiceProvider? services, Func<Type?> part, CancellationToken cancellationToken = default)
    {
        var source = services?.GetService<IContextValueSource>();
        return source is null ? Empty : new(source, part, cancellationToken);
    }

    /// <summary>
    /// Reads a value, telling an absent value from one that is present.
    /// </summary>
    /// <param name="name">The name of the value.</param>
    /// <returns>The <see cref="ContextValueRead"/>.</returns>
    /// <exception cref="RequiredContextValueAbsentException">The value is declared as required and is absent.</exception>
    /// <exception cref="ContextValueUnavailableException">The value could not be resolved.</exception>
    public ValueTask<ContextValueRead> ReadAsync(string name) =>
        _source is null ? ValueTask.FromResult(ContextValueRead.Absent) : _source.Read(_part(), name, _cancellationToken);

    /// <summary>
    /// Gets a value.
    /// </summary>
    /// <param name="name">The name of the value.</param>
    /// <returns>The value, or <see langword="null"/> when it is absent.</returns>
    /// <exception cref="RequiredContextValueAbsentException">The value is declared as required and is absent.</exception>
    /// <exception cref="ContextValueUnavailableException">The value could not be resolved.</exception>
    public async ValueTask<object?> GetAsync(string name) => (await ReadAsync(name)).Value;

    /// <summary>
    /// Gets a value.
    /// </summary>
    /// <param name="name">The name of the value.</param>
    /// <returns>The value, or <see langword="null"/> when it is absent.</returns>
    /// <exception cref="RequiredContextValueAbsentException">The value is declared as required and is absent.</exception>
    /// <exception cref="ContextValueUnavailableException">The value could not be resolved.</exception>
    public object? Get(string name) => GetAsync(name).AsTask().GetAwaiter().GetResult();

    /// <summary>
    /// Gets a value of a type.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="name">The name of the value.</param>
    /// <returns>The value, or the default of <typeparamref name="T"/> when it is absent.</returns>
    /// <exception cref="ContextValuesTypeMismatch">The value is not a <typeparamref name="T"/>.</exception>
    public T? Get<T>(string name) => Get(name) switch
    {
        null => default,
        T typed => typed,
        var value => throw new ContextValuesTypeMismatch(typeof(T), $"the value '{name}' is a '{value.GetType()}'.")
    };

    /// <summary>
    /// Reads the values as a statically typed payload without dynamic dispatch.
    /// </summary>
    /// <typeparam name="T">A type whose public properties, or constructor parameters, are named after the provided values.</typeparam>
    /// <returns>The values as <typeparamref name="T"/>.</returns>
    /// <exception cref="ContextValuesTypeMismatch">The values do not fit <typeparamref name="T"/>.</exception>
    public T ValuesAs<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] T>()
    {
        if (this is T self)
        {
            return self;
        }

        var type = typeof(T);
        var constructor = type.GetConstructors().OrderByDescending(_ => _.GetParameters().Length).FirstOrDefault()
            ?? throw new ContextValuesTypeMismatch(type, "it has no public constructor.");
        var parameters = constructor.GetParameters();
        var arguments = parameters.Select(_ => ConvertTo(_.ParameterType, _.Name!, Lookup(_.Name!))).ToArray();
        var instance = constructor.Invoke(arguments);
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(_ => _.CanWrite && _.GetIndexParameters().Length == 0 && !parameters.Any(p => string.Equals(p.Name, _.Name, StringComparison.OrdinalIgnoreCase))))
        {
            property.SetValue(instance, ConvertTo(property.PropertyType, property.Name, Lookup(property.Name)));
        }

        return (T)instance;
    }

    /// <summary>
    /// Gets the values as a dynamic object, so a value reads by name such as <c>values.Vessel</c>.
    /// </summary>
    /// <returns>The dynamic view of the values.</returns>
    [RequiresDynamicCode("Reading a value by name uses dynamic dispatch. Use Get, GetAsync or ValuesAs where dynamic code is unavailable.")]
    public dynamic AsDynamic() => new DynamicContextValues(this);

    /// <summary>
    /// Gets a value by name, trying the name with its first letter in lower case first.
    /// </summary>
    /// <param name="name">The name of the value.</param>
    /// <returns>The value, or <see langword="null"/> when it is absent.</returns>
    internal object? Lookup(string name)
    {
        var camel = Camel(name);
        var value = Get(camel);
        return value is null && camel != name ? Get(name) : value;
    }

    static string Camel(string name) => name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];

    static object? ConvertTo(Type type, string name, object? value)
    {
        if (value is null)
        {
            return type.IsValueType && Nullable.GetUnderlyingType(type) is null
                ? throw new ContextValuesTypeMismatch(type, $"the value '{name}' is absent and '{type}' cannot hold that. Make it nullable.")
                : null;
        }

        return type.IsInstanceOfType(value)
            ? value
            : throw new ContextValuesTypeMismatch(type, $"the value '{name}' is a '{value.GetType()}'.");
    }
}
