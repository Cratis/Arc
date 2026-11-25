// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Adapts compiler-declared native union cases without treating ordinary constructors as alternatives.
/// </summary>
static class NativeUnionSymbols
{
    /// <summary>
    /// The bound for recursive response signatures and visible expressions.
    /// </summary>
    internal const int MaximumDepth = 32;

    /// <summary>
    /// Recognizes the native union contract without depending on experimental compiler APIs.
    /// </summary>
    /// <param name="type">The candidate type.</param>
    /// <returns>Whether the type implements the runtime union contract.</returns>
    internal static bool IsUnion(ITypeSymbol? type) => type?.AllInterfaces.Any(contract =>
        contract.ToDisplayString() == "System.Runtime.CompilerServices.IUnion") == true;

    /// <summary>
    /// Gets generated case parameter types, preserving generic substitutions and nullable annotations.
    /// </summary>
    /// <param name="type">The candidate union.</param>
    /// <returns>The cases, or a default array when this is not a native union.</returns>
    internal static ImmutableArray<ITypeSymbol> Cases(ITypeSymbol type) => type is INamedTypeSymbol union && IsUnion(union)
        ? [.. union.InstanceConstructors.Where(IsCaseConstructor).Select(constructor => constructor.Parameters[0].Type)]
        : default;

    /// <summary>
    /// Checks the source or metadata constructor shape shared with runtime and proxy generation.
    /// </summary>
    /// <param name="constructor">The constructor to inspect.</param>
    /// <returns>Whether the constructor declares a native union case.</returns>
    internal static bool IsCaseConstructor(IMethodSymbol constructor) => constructor.DeclaredAccessibility == Accessibility.Public &&
        UnionCaseConvention.IsCaseConstructor(
            constructor.IsImplicitlyDeclared || constructor.GetAttributes().Any(attribute =>
                attribute.AttributeClass?.ToDisplayString() == UnionCaseConvention.CompilerGeneratedAttributeName),
            constructor.Parameters.Length);
}
