// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Compares types declared once but seen through the references of different compilations.
/// </summary>
static class DeclaredTypeIdentity
{
    /// <summary>
    /// Determines whether two symbols describe the same declared type.
    /// </summary>
    /// <param name="left">The first type.</param>
    /// <param name="right">The second type.</param>
    /// <returns><see langword="true"/> when they are equal, or come from the same assembly with the same fully qualified name.</returns>
    public static bool IsSame(INamedTypeSymbol? left, INamedTypeSymbol? right) =>
        SymbolEqualityComparer.Default.Equals(left, right) ||
        (left is not null && right is not null &&
         left.ContainingAssembly is { } leftAssembly && right.ContainingAssembly is { } rightAssembly &&
         leftAssembly.Identity.Equals(rightAssembly.Identity) &&
         string.Equals(
             left.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
             right.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
             StringComparison.Ordinal));
}
