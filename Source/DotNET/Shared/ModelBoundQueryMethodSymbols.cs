// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// Mirrors model-bound query exclusions for compiler and source-analysis discovery.
/// </summary>
internal static class ModelBoundQueryMethodSymbols
{
    /// <summary>
    /// Determines whether a method is eligible for query return-type validation.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <returns>Whether the method is an ordinary public or internal static query candidate.</returns>
    internal static bool IsCandidate(IMethodSymbol method) =>
        method.MethodKind == MethodKind.Ordinary &&
        method.IsStatic &&
        method.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal &&
        method.TypeParameters.Length == 0 &&
        !method.GetAttributes().Any(_ => _.AttributeClass?.ToDisplayString() == "System.Runtime.CompilerServices.CompilerGeneratedAttribute") &&
        !IsExcluded(method);

    /// <summary>
    /// Determines whether a method is explicitly opted out or is a recognizable fold creator.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <returns>Whether query discovery must ignore the method.</returns>
    internal static bool IsExcluded(IMethodSymbol method) =>
        method.GetAttributes().Any(_ => HasFullName(_.AttributeClass, "Cratis.Arc.Queries.ModelBound.NotAQueryAttribute")) ||
        (IsCreator(method) &&
            (method.Parameters.Any(_ => HasFullName(UnwrapNullable(_.Type), "Cratis.Chronicle.Events.EventContext")) ||
                IsEvent(UnwrapNullable(method.Parameters[0].Type))));

    /// <summary>
    /// Determines whether a method has the static fold-creator shape.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <returns>Whether it is named Create, accepts one or two parameters, and returns its declaring type.</returns>
    internal static bool IsCreator(IMethodSymbol method) =>
        method.IsStatic && method.Name == "Create" &&
        method.Parameters.Length is 1 or 2 && ReturnsDeclaringType(method);

    static bool ReturnsDeclaringType(IMethodSymbol method)
    {
        var type = method.ReturnType;
        if (type is INamedTypeSymbol { TypeArguments.Length: 1 } named &&
            named.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks" &&
            (named.Name == "Task" || named.Name == "ValueTask"))
        {
            type = named.TypeArguments[0];
        }

        return SymbolEqualityComparer.Default.Equals(type, method.ContainingType);
    }

    static ITypeSymbol UnwrapNullable(ITypeSymbol type) =>
        type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable
            ? nullable.TypeArguments[0]
            : type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);

    static bool HasFullName(ITypeSymbol? type, string fullName) =>
        type is { TypeKind: not TypeKind.Error } && type.ToDisplayString() == fullName;

    static bool IsEvent(ITypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.GetAttributes().Any(_ => HasFullName(_.AttributeClass, "Cratis.Chronicle.Events.EventTypeAttribute")))
            {
                return true;
            }
        }

        return false;
    }
}
