// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Generators;

/// <summary>
/// Decides which types generated code can name.
/// </summary>
internal static class TypeNaming
{
    /// <summary>
    /// Determines whether generated code in a compilation can name a type without reporting a diagnostic.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="compilation">The compilation the generated code is emitted into.</param>
    /// <returns>Whether generated code can name the type.</returns>
    public static bool CanBeNamed(ITypeSymbol type, Compilation compilation) => type switch
    {
        IArrayTypeSymbol array => CanBeNamed(array.ElementType, compilation),
        INamedTypeSymbol named => !IsFileLocalOrFlagged(named) && compilation.IsSymbolAccessibleWithin(named, compilation.Assembly) &&
            named.TypeArguments.All(argument => CanBeNamed(argument, compilation)) &&
            (named.ContainingType is null || CanBeNamed(named.ContainingType, compilation)),
        _ => false
    };

    /// <summary>
    /// Determines whether an attribute flags what it is applied to as obsolete or experimental.
    /// </summary>
    /// <param name="attribute">The full name of the attribute type.</param>
    /// <returns>Whether the attribute is a flag.</returns>
    public static bool IsFlag(string? attribute) =>
        string.Equals(attribute, "System.ObsoleteAttribute", StringComparison.Ordinal) ||
        string.Equals(attribute, "System.Diagnostics.CodeAnalysis.ExperimentalAttribute", StringComparison.Ordinal);

    /// <summary>
    /// Naming a file-local type from generated code fails, and naming an obsolete or experimental type reports a
    /// warning or error in the consumer's build.
    /// </summary>
    /// <param name="type">The type to check, with its containing types.</param>
    /// <returns>Whether generated code must not name the type.</returns>
    static bool IsFileLocalOrFlagged(INamedTypeSymbol type) =>
        type.IsFileLocal ||
        type.GetAttributes().Any(attribute => IsFlag(attribute.AttributeClass?.ToDisplayString())) ||
        (type.ContainingType is not null && IsFileLocalOrFlagged(type.ContainingType));
}
