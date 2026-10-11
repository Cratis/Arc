// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// Recognizes conventional return shapes for the whole-type query migration code fix.
/// </summary>
internal static class ModelBoundQueryReturnTypes
{
    /// <summary>
    /// Checks a single task wrapper followed by a model, collection, stream, or subject shape.
    /// </summary>
    /// <param name="type">The declared result.</param>
    /// <param name="owner">The declaring read model.</param>
    /// <returns>Whether the return shape is supported.</returns>
    internal static bool IsSupported(ITypeSymbol type, INamedTypeSymbol owner)
    {
        var result = UnwrapTask(type);
        return IsModelResult(result, owner) || IsAsyncSequence(result, owner) || IsSubjectResult(result, owner);
    }

    static ITypeSymbol UnwrapTask(ITypeSymbol type) => type is INamedTypeSymbol task && IsGeneric(task, "System.Threading.Tasks.Task`1")
        ? task.TypeArguments[0]
        : type;

    static bool IsModelResult(ITypeSymbol type, INamedTypeSymbol owner) =>
        SymbolEqualityComparer.Default.Equals(type, owner) || IsCollection(type, owner);

    static bool IsAsyncSequence(ITypeSymbol type, INamedTypeSymbol owner) =>
        type is INamedTypeSymbol stream && IsGeneric(stream, "System.Collections.Generic.IAsyncEnumerable`1") &&
        SymbolEqualityComparer.Default.Equals(stream.TypeArguments[0], owner);

    static bool IsSubjectResult(ITypeSymbol type, INamedTypeSymbol owner) =>
        type is INamedTypeSymbol subject && IsGeneric(subject, "System.Reactive.Subjects.ISubject`1") &&
        IsModelResult(subject.TypeArguments[0], owner);

    static bool IsCollection(ITypeSymbol type, INamedTypeSymbol owner)
    {
        if (type is IArrayTypeSymbol array)
        {
            return SymbolEqualityComparer.Default.Equals(array.ElementType, owner);
        }

        return type is INamedTypeSymbol named && new[] { named }.Concat(named.AllInterfaces)
            .Any(candidate => IsEnumerableOf(candidate, owner));
    }

    static bool IsEnumerableOf(INamedTypeSymbol type, INamedTypeSymbol owner) =>
        IsGeneric(type, "System.Collections.Generic.IEnumerable`1") && SymbolEqualityComparer.Default.Equals(type.TypeArguments[0], owner);

    static bool IsGeneric(INamedTypeSymbol type, string name) => $"{type.ContainingNamespace.ToDisplayString()}.{type.MetadataName}" == name;
}
