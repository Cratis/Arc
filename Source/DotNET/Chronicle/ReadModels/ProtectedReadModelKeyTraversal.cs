// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Validation;
using Cratis.Concepts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Checks raw key parts independently of missing-model policy and reader acquisition.
/// </summary>
/// <param name="options">Composite coverage options.</param>
/// <param name="logger">The fallback warning logger.</param>
internal sealed class ProtectedReadModelKeyTraversal(
    IOptions<ProtectedReadModelKeyOptions> options,
    ILogger<ProtectedReadModelKeyTraversal> logger)
{
    static readonly ConcurrentDictionary<Type, byte> _warnedTypes = new();
    static readonly ConcurrentDictionary<Type, Func<object, object?>[]> _fallbackReaders = new();

    /// <summary>
    /// Gets whether dynamic code is available; the instance-local seam permits restricted-runtime specifications.
    /// </summary>
    internal bool DynamicCodeSupported { get; init; } = RuntimeFeature.IsDynamicCodeSupported;

    bool Strict => options.Value.Strict || !DynamicCodeSupported;

    /// <summary>
    /// Checks already-resolved wire text without converting or changing it.
    /// </summary>
    /// <param name="wireText">The target text.</param>
    /// <returns>Whether the wire text is usable.</returns>
    internal static bool IsValidWireText(string wireText) => !IsRefusedText(wireText);

    /// <summary>
    /// Checks a raw key and then its already-resolved wire text, without converting or rerouting the target.
    /// </summary>
    /// <param name="rawKey">The key before conversion.</param>
    /// <param name="wireText">The resolved target text.</param>
    /// <returns>Whether both the raw parts and the wire text are usable.</returns>
    internal bool IsValid(object? rawKey, string wireText) => IsValidRawKey(rawKey) && IsValidWireText(wireText);

    /// <summary>
    /// Checks raw parts before a caller converts the key. Missing-model rejection belongs to the caller.
    /// </summary>
    /// <param name="rawKey">The original key value.</param>
    /// <returns>Whether all raw parts are usable.</returns>
    internal bool IsValidRawKey(object? rawKey) => !IsRefused(rawKey, new(ReferenceEqualityComparer.Instance));

    /// <summary>
    /// Checks coverage for a statically declared type without enumerating its properties.
    /// </summary>
    /// <param name="type">The declared key type.</param>
    internal void EnsureCoverage(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (!IsScalar(type) && !type.IsConcept() && !typeof(ITuple).IsAssignableFrom(type) &&
            type != typeof(object) && !type.IsInterface && !type.IsAbstract)
        {
            RequireAccessors(type);
        }
    }

    static bool IsRefusedText(string text) =>
        string.IsNullOrWhiteSpace(text) || text == "*" || text.Contains('#', StringComparison.Ordinal) ||
        char.IsWhiteSpace(text[0]) || char.IsWhiteSpace(text[^1]);

    static bool IsScalar(Type type) =>
        type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) ||
        type == typeof(Guid) || type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
        type == typeof(DateOnly) || type == typeof(TimeOnly) || type == typeof(TimeSpan);

    bool IsRefused(object? value, HashSet<object> visited)
    {
        while (value?.IsConcept() == true)
        {
            value = value.GetConceptValue();
        }
        if (value is null) return true;
        if (value is string text) return IsRefusedText(text);
        if (GeneratedIdentifierCanonicalizer.Canonicalize(value) is not null) return false;
        if (!value.GetType().IsValueType && !visited.Add(value)) return false;
        if (value is ITuple tuple)
        {
            for (var index = 0; index < tuple.Length; index++)
            {
                if (IsRefused(tuple[index], visited)) return true;
            }

            return false;
        }

        return ReadersFor(value.GetType()).Any(read => IsRefused(read(value), visited));
    }

    Func<object, object?>[] ReadersFor(Type type)
    {
        if (ProtectedReadModelKeyAccessors.TryGet(type, out var readers)) return readers;
        RequireAccessors(type);

        return _fallbackReaders.GetOrAdd(type, static keyType =>
            [.. ModelGraphValidator.GetWalkablePropertiesThroughReflection(keyType, publicGettersOnly: true).Select(member => member.Read)]);
    }

    void RequireAccessors(Type type)
    {
        if (ProtectedReadModelKeyAccessors.TryGet(type, out _)) return;
        if (Strict) throw new ProtectedReadModelKeyAccessorMissing(type);
        if (logger.IsEnabled(LogLevel.Warning) && _warnedTypes.TryAdd(type, 0))
        {
            logger.ReflectionFallback(type.FullName ?? type.Name);
        }
    }
}
