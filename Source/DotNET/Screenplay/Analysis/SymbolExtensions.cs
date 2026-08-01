// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Extends Roslyn symbols with the matching every recognizer needs.
/// </summary>
/// <remarks>
/// Everything is matched on the fully qualified metadata name rather than on symbol identity, which is what lets
/// the generator recognize Arc and Chronicle artifacts without referencing either.
/// </remarks>
public static class SymbolExtensions
{
    /// <summary>
    /// Gets the fully qualified metadata name of a type, including the arity of a generic type.
    /// </summary>
    /// <param name="symbol">The type to name.</param>
    /// <returns>The name, for example <c>Cratis.Concepts.ConceptAs`1</c>.</returns>
    public static string FullMetadataName(this INamedTypeSymbol symbol)
    {
        var definition = symbol.OriginalDefinition;

        return definition.ContainingNamespace is { IsGlobalNamespace: false } containing
            ? $"{containing.ToDisplayString()}.{definition.MetadataName}"
            : definition.MetadataName;
    }

    /// <summary>
    /// Determines whether a type is the named one.
    /// </summary>
    /// <param name="symbol">The type to check.</param>
    /// <param name="fullMetadataName">The fully qualified metadata name to match.</param>
    /// <returns>True when the type matches.</returns>
    public static bool Is(this ITypeSymbol? symbol, string fullMetadataName) =>
        symbol is INamedTypeSymbol named && string.Equals(named.FullMetadataName(), fullMetadataName, StringComparison.Ordinal);

    /// <summary>
    /// Gets the attribute of a given type applied to a symbol.
    /// </summary>
    /// <param name="symbol">The symbol to read.</param>
    /// <param name="fullMetadataName">The fully qualified metadata name of the attribute.</param>
    /// <returns>The attribute, or <see langword="null"/> when it is not applied.</returns>
    public static AttributeData? GetAttribute(this ISymbol symbol, string fullMetadataName) =>
        symbol.GetAttributes().FirstOrDefault(_ => _.AttributeClass.Is(fullMetadataName));

    /// <summary>
    /// Gets every attribute of a given type applied to a symbol.
    /// </summary>
    /// <param name="symbol">The symbol to read.</param>
    /// <param name="fullMetadataName">The fully qualified metadata name of the attribute.</param>
    /// <returns>The attributes.</returns>
    public static IEnumerable<AttributeData> GetAttributes(this ISymbol symbol, string fullMetadataName) =>
        symbol.GetAttributes().Where(_ => _.AttributeClass.Is(fullMetadataName));

    /// <summary>
    /// Determines whether an attribute of a given type is applied to a symbol.
    /// </summary>
    /// <param name="symbol">The symbol to check.</param>
    /// <param name="fullMetadataName">The fully qualified metadata name of the attribute.</param>
    /// <returns>True when the attribute is applied.</returns>
    public static bool HasAttribute(this ISymbol symbol, string fullMetadataName) =>
        symbol.GetAttribute(fullMetadataName) is not null;

    /// <summary>
    /// Finds a base type of a type, walking the whole chain.
    /// </summary>
    /// <param name="symbol">The type to walk from.</param>
    /// <param name="fullMetadataName">The fully qualified metadata name of the base type.</param>
    /// <returns>The base type, or <see langword="null"/> when the type does not derive from it.</returns>
    public static INamedTypeSymbol? FindBase(this ITypeSymbol? symbol, string fullMetadataName)
    {
        for (var current = symbol?.BaseType; current is not null; current = current.BaseType)
        {
            if (current.Is(fullMetadataName))
            {
                return current;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds an interface a type implements.
    /// </summary>
    /// <param name="symbol">The type to check.</param>
    /// <param name="fullMetadataName">The fully qualified metadata name of the interface.</param>
    /// <returns>The interface, or <see langword="null"/> when the type does not implement it.</returns>
    public static INamedTypeSymbol? FindInterface(this ITypeSymbol? symbol, string fullMetadataName) =>
        symbol?.AllInterfaces.FirstOrDefault(_ => _.Is(fullMetadataName));

    /// <summary>
    /// Gets the value of a named argument of an attribute.
    /// </summary>
    /// <param name="attribute">The attribute to read.</param>
    /// <param name="name">The name of the argument.</param>
    /// <returns>The value, or <see langword="null"/> when the argument was not given.</returns>
    public static object? GetNamedArgument(this AttributeData attribute, string name) =>
        attribute.NamedArguments.FirstOrDefault(_ => string.Equals(_.Key, name, StringComparison.Ordinal)).Value.Value;

    /// <summary>
    /// Gets the value of a constructor argument of an attribute.
    /// </summary>
    /// <param name="attribute">The attribute to read.</param>
    /// <param name="index">The position of the argument.</param>
    /// <returns>The value, or <see langword="null"/> when the argument was not given.</returns>
    public static object? GetArgument(this AttributeData attribute, int index) =>
        attribute.ConstructorArguments.Length > index ? attribute.ConstructorArguments[index].Value : null;

    /// <summary>
    /// Gets a constructor argument of an attribute together with the type it was written as.
    /// </summary>
    /// <param name="attribute">The attribute to read.</param>
    /// <param name="index">The position of the argument.</param>
    /// <returns>The argument, or <see langword="null"/> when it was not given.</returns>
    /// <remarks>
    /// The value alone is all most readers need. A constant of an enumeration is the exception - it arrives as the
    /// number behind a member, and only the type it was written as says which enumeration that member belongs to.
    /// </remarks>
    public static TypedConstant? GetTypedArgument(this AttributeData attribute, int index) =>
        attribute.ConstructorArguments.Length > index ? attribute.ConstructorArguments[index] : null;

    /// <summary>
    /// Gets the public instance properties a type carries, including the ones it inherits.
    /// </summary>
    /// <param name="symbol">The type to read.</param>
    /// <returns>The properties, base types first and in declaration order within each type.</returns>
    /// <remarks>
    /// A property declared on a base record is part of what the type carries just as much as one declared on the type
    /// itself - it is serialized, it is sent, and a document leaving it out describes a shape that does not exist.
    /// <para>
    /// Within one type the order the source declares them in is kept, since that is what the developer wrote. Roslyn
    /// returns the members of a type split across several partial declarations in the order the syntax trees were
    /// handed to the compiler, which a build is free to vary, so the declarations are ordered by the file they live
    /// in first. That is what makes the same source produce the same document however it was globbed.
    /// </para>
    /// </remarks>
    public static IEnumerable<IPropertySymbol> DeclaredProperties(this ITypeSymbol symbol)
    {
        var declaring = new List<ITypeSymbol>();
        for (var current = symbol; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            declaring.Insert(0, current);
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);

        return declaring
            .SelectMany(InDeclarationOrder)
            .Where(_ => seen.Add(_.Name));
    }

    /// <summary>
    /// Gets the path of the file a symbol is declared in.
    /// </summary>
    /// <param name="symbol">The symbol to locate.</param>
    /// <returns>The path, or <see langword="null"/> when the symbol has no source.</returns>
    /// <remarks>
    /// A type declared across several files has as many paths as it has declarations, and which one Roslyn hands over
    /// first follows the order the syntax trees arrived in. The first in order is taken so that the document names
    /// the same file every time.
    /// </remarks>
    public static string? SourceFilePath(this ISymbol symbol) =>
        symbol.DeclaringSyntaxReferences
            .Select(_ => _.SyntaxTree.FilePath)
            .Where(_ => !string.IsNullOrWhiteSpace(_))
            .Order(StringComparer.Ordinal)
            .FirstOrDefault();

    /// <summary>
    /// Gets the namespace a symbol lives in.
    /// </summary>
    /// <param name="symbol">The symbol to read.</param>
    /// <returns>The namespace, empty for the global namespace.</returns>
    public static string Namespace(this ISymbol symbol) =>
        symbol.ContainingNamespace is { IsGlobalNamespace: false } @namespace ? @namespace.ToDisplayString() : string.Empty;

    /// <summary>
    /// Gets the public instance properties one type declares, in the order its source declares them.
    /// </summary>
    /// <param name="symbol">The type to read.</param>
    /// <returns>The properties.</returns>
    static IEnumerable<IPropertySymbol> InDeclarationOrder(ITypeSymbol symbol) =>
        symbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(_ => _.DeclaredAccessibility == Accessibility.Public && !_.IsStatic && !_.IsIndexer && _.Name != "EqualityContract")
            .OrderBy(DeclaredIn, StringComparer.Ordinal);

    /// <summary>
    /// Gets the file a property is declared in, for ordering the members of a type declared across several of them.
    /// </summary>
    /// <param name="property">The property to locate.</param>
    /// <returns>The path, empty when the property has no source.</returns>
    static string DeclaredIn(IPropertySymbol property) => property.SourceFilePath() ?? string.Empty;
}
