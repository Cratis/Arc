// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Arc.Queries;

/// <summary>
/// Holds the <see cref="QueryableKeyType"/> of the key types that observable query sets compare, without closing generic
/// types over a type only known at runtime.
/// </summary>
/// <remarks>
/// Primitive types are registered up front, the key types of generated sort registrations are registered through
/// <see cref="QueryableElementTypes.RegisterSortKey{TElement, TKey}"/>, and reference types are closed through a
/// class-constrained generic method, which the runtime shares code for. Other value types need a registration when the
/// code is ahead-of-time compiled.
/// </remarks>
internal static class QueryableKeyTypes
{
    static readonly ConcurrentDictionary<Type, QueryableKeyType> _keyTypes = new();

    static QueryableKeyTypes()
    {
        RegisterWithNullable<bool>();
        RegisterWithNullable<byte>();
        RegisterWithNullable<sbyte>();
        RegisterWithNullable<short>();
        RegisterWithNullable<ushort>();
        RegisterWithNullable<int>();
        RegisterWithNullable<uint>();
        RegisterWithNullable<long>();
        RegisterWithNullable<ulong>();
        RegisterWithNullable<float>();
        RegisterWithNullable<double>();
        RegisterWithNullable<decimal>();
        RegisterWithNullable<char>();
        RegisterWithNullable<Guid>();
        RegisterWithNullable<DateTime>();
        RegisterWithNullable<DateTimeOffset>();
        RegisterWithNullable<DateOnly>();
        RegisterWithNullable<TimeOnly>();
        RegisterWithNullable<TimeSpan>();
        Register<string>();
    }

    /// <summary>
    /// Registers a key type. The first registration is kept.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    internal static void Register<TKey>() =>
        _keyTypes.TryAdd(typeof(TKey), new QueryableKeyTypeFor<TKey>());

    /// <summary>
    /// Gets the <see cref="QueryableKeyType"/> of a key type.
    /// </summary>
    /// <param name="elementType">The element type of the queryable the key belongs to.</param>
    /// <param name="keyType">The key type.</param>
    /// <returns>The <see cref="QueryableKeyType"/> for the key type.</returns>
    /// <exception cref="MissingQueryableSortKey">Thrown when the key type is a value type that is not registered.</exception>
    internal static QueryableKeyType For(Type elementType, Type keyType)
    {
        if (_keyTypes.TryGetValue(keyType, out var keyTypeRegistration))
        {
            return keyTypeRegistration;
        }

#if NET9_0_OR_GREATER
        if (!keyType.IsValueType)
        {
            return _keyTypes.GetOrAdd(keyType, CreateForReferenceType);
        }
#endif

        throw new MissingQueryableSortKey(elementType, keyType);
    }

    static void RegisterWithNullable<TKey>()
        where TKey : struct
    {
        Register<TKey>();
        Register<TKey?>();
    }

#if NET9_0_OR_GREATER
    static QueryableKeyType CreateForReferenceType(Type keyType) =>
        (QueryableKeyType)typeof(QueryableKeyTypes)
            .GetMethod(nameof(CreateForReferenceTypeOf), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(keyType)
            .Invoke(null, null)!;

    static QueryableKeyTypeFor<TKey> CreateForReferenceTypeOf<TKey>()
        where TKey : class => new();
#endif
}
