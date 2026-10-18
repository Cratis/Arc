// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Queries;

namespace Cratis.Arc.Validation;

/// <summary>
/// Holds generated data annotation metadata for exact model types and model-bound query identities.
/// </summary>
/// <remarks>
/// Not intended to be called directly. Types without generated metadata retain reflection-based validation.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class DataAnnotationMembers
{
    static readonly ConcurrentDictionary<Type, (DataAnnotationMember[] Members, Func<ValidationAttribute[]> Attributes)> _types = new();
    static readonly ConcurrentDictionary<FullyQualifiedQueryName, DataAnnotationMember[]> _queries = new();

    /// <summary>
    /// Registers all annotated properties of an exact type.
    /// </summary>
    /// <param name="type">The exact runtime type.</param>
    /// <param name="members">The annotated properties.</param>
    public static void Register(Type type, DataAnnotationMember[] members) => Register(type, members, static () => []);

    /// <summary>
    /// Registers the complete property and type-level validation metadata for an exact type.
    /// </summary>
    /// <param name="type">The exact runtime type.</param>
    /// <param name="members">All annotated properties, including inherited properties.</param>
    /// <param name="attributes">Lazily reads the type-level validation attributes.</param>
    public static void Register(Type type, DataAnnotationMember[] members, Func<ValidationAttribute[]> attributes) => _types[type] = (members, attributes);

    /// <summary>
    /// Registers annotated parameters of a model-bound query.
    /// </summary>
    /// <param name="query">The fully qualified query identity.</param>
    /// <param name="members">The annotated parameters.</param>
    public static void RegisterQueryParameters(FullyQualifiedQueryName query, DataAnnotationMember[] members) => _queries[query] = members;

    /// <summary>
    /// Gets annotation metadata for an exact runtime type.
    /// </summary>
    /// <param name="type">The runtime type.</param>
    /// <param name="members">The registered members.</param>
    /// <returns>Whether metadata was registered.</returns>
    internal static bool TryGet(Type type, [NotNullWhen(true)] out DataAnnotationMember[]? members) =>
        TryGet(type, out members, out _);

    /// <summary>
    /// Gets the complete annotation metadata for an exact runtime type.
    /// </summary>
    /// <param name="type">The runtime type.</param>
    /// <param name="members">The registered members.</param>
    /// <param name="attributes">The type-level attribute reader.</param>
    /// <returns>Whether metadata was registered.</returns>
    internal static bool TryGet(Type type, [NotNullWhen(true)] out DataAnnotationMember[]? members, [NotNullWhen(true)] out Func<ValidationAttribute[]>? attributes)
    {
        var found = _types.TryGetValue(type, out var metadata);
        members = found ? metadata.Members : null;
        attributes = found ? metadata.Attributes : null;
        return found;
    }

    /// <summary>
    /// Gets annotation metadata for a query's parameters.
    /// </summary>
    /// <param name="query">The query identity.</param>
    /// <param name="members">The registered parameters.</param>
    /// <returns>Whether metadata was registered.</returns>
    internal static bool TryGet(FullyQualifiedQueryName query, [NotNullWhen(true)] out DataAnnotationMember[]? members) =>
        _queries.TryGetValue(query, out members);
}
