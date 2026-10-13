// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// Adapts query selection to Roslyn without changing legacy discovery predicates.
/// </summary>
internal static class ModelBoundQueryMethodSymbols
{
    /// <summary>
    /// Full metadata name of the owner attribute.
    /// </summary>
    internal const string ReadModelName = QuerySelectionPolicy.ReadModelName;

    /// <summary>
    /// Full metadata name of the method marker.
    /// </summary>
    internal const string QueryName = QuerySelectionPolicy.QueryName;

    /// <summary>
    /// Actionable validation error shared by analyzers and generators.
    /// </summary>
    internal static readonly DiagnosticDescriptor InvalidDeclaration = new(
        "ARC0026", "Invalid explicit query declaration", "Query '{0}': {1}", "Arc", DiagnosticSeverity.Error, true);

    /// <summary>
    /// Tests the legacy generator candidate shape.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <returns>Whether it is a candidate.</returns>
    internal static bool IsCandidate(IMethodSymbol method) =>
        method.MethodKind == MethodKind.Ordinary && method.IsStatic &&
        method.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal &&
        method.TypeParameters.Length == 0 &&
        !method.GetAttributes().Any(_ => _.AttributeClass?.ToDisplayString() == "System.Runtime.CompilerServices.CompilerGeneratedAttribute");

    /// <summary>
    /// Reads the Arc marker by its full metadata name.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <returns>Whether it is marked.</returns>
    internal static bool IsMarked(IMethodSymbol method) =>
        method.GetAttributes().Any(_ => _.AttributeClass?.ToDisplayString() == QueryName);

    /// <summary>
    /// Reads the nearest read-model declaration, not the enclosing type's setting.
    /// </summary>
    /// <param name="type">The owner.</param>
    /// <returns>The nearest declaration.</returns>
    internal static AttributeData? ReadModel(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var attribute = current.GetAttributes().FirstOrDefault(_ => _.AttributeClass?.ToDisplayString() == ReadModelName);
            if (attribute is not null)
            {
                return attribute;
            }
        }

        return null;
    }

    /// <summary>
    /// Reads the opt-in setting across partial declarations and base types.
    /// </summary>
    /// <param name="type">The owner.</param>
    /// <returns>Whether explicit selection is required.</returns>
    internal static bool RequiresQueryAttribute(INamedTypeSymbol type) =>
        ReadModel(type)?.NamedArguments.Any(_ => _.Key == "RequireQueryAttribute" && _.Value.Value is true) == true;

    /// <summary>
    /// Applies opt-in selection without narrowing legacy methods.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <returns>Whether selection permits it.</returns>
    internal static bool IsSelected(IMethodSymbol method) =>
        QuerySelectionPolicy.IsSelected(HasSelfParameter(method), RequiresQueryAttribute(method.ContainingType), () => IsMarked(method), () => IsValidExplicitQuery(method));

    /// <summary>
    /// Selects queries with explicit rules or the caller's unchanged legacy predicate.
    /// </summary>
    /// <param name="type">The owner.</param>
    /// <param name="legacyPredicate">The legacy predicate.</param>
    /// <returns>The selected queries.</returns>
    internal static IEnumerable<IMethodSymbol> GetMethods(INamedTypeSymbol type, Func<IMethodSymbol, bool> legacyPredicate) =>
        type.GetMembers().OfType<IMethodSymbol>()
            .Where(method => IsSelected(method) && legacyPredicate(method));

    /// <summary>
    /// Validates markers before discovery and rejects ambiguous selected names.
    /// </summary>
    /// <param name="type">The owner.</param>
    /// <param name="legacyPredicate">The surface's conventional return predicate.</param>
    /// <returns>Actionable declaration errors.</returns>
    internal static IEnumerable<Diagnostic> Validate(INamedTypeSymbol type, Func<IMethodSymbol, bool> legacyPredicate)
    {
        var methods = type.GetMembers().OfType<IMethodSymbol>().ToArray();
        foreach (var diagnostic in InvalidMarkers(type, methods, legacyPredicate).Concat(DuplicateSelectedNames(type, methods, legacyPredicate)))
        {
            yield return diagnostic;
        }
    }

    /// <summary>
    /// Rejects invalid intent when a tooling reader cannot emit compiler diagnostics.
    /// </summary>
    /// <param name="type">The owner.</param>
    /// <param name="legacyPredicate">The surface's conventional return predicate.</param>
    /// <exception cref="InvalidQueryDeclaration">Explicit query intent is invalid or ambiguous.</exception>
    internal static void EnsureValid(INamedTypeSymbol type, Func<IMethodSymbol, bool> legacyPredicate)
    {
        var diagnostics = Validate(type, legacyPredicate).ToArray();
        if (diagnostics.Length > 0)
        {
            throw new InvalidQueryDeclaration(string.Join(" | ", diagnostics.Select(_ => _.GetMessage())));
        }
    }

    /// <summary>
    /// Validates structural explicit query rules independently of the surface's conventional return predicate.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <returns>Whether it is eligible in explicit mode.</returns>
    internal static bool IsValidExplicitQuery(IMethodSymbol method) =>
        IsCandidate(method) && !IsOpen(method.ContainingType) && !method.ReturnsByRef && !method.ReturnsByRefReadonly &&
        method.Parameters.All(_ => _.RefKind == RefKind.None) && !HasSelfParameter(method);

    /// <summary>
    /// Detects an exact self parameter, ignoring nullable reference annotations.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <returns>Whether any parameter is the declaring model itself.</returns>
    internal static bool HasSelfParameter(IMethodSymbol method) => method.Parameters.Any(parameter =>
        SymbolEqualityComparer.Default.Equals(UnwrapNullable(parameter.Type), method.ContainingType));

    static ITypeSymbol UnwrapNullable(ITypeSymbol type) =>
        type is INamedTypeSymbol named && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            ? named.TypeArguments[0]
            : type;

    static IEnumerable<Diagnostic> InvalidMarkers(INamedTypeSymbol type, IEnumerable<IMethodSymbol> methods, Func<IMethodSymbol, bool> legacyPredicate) => methods
        .Where(method => IsMarked(method) && HasInvalidDeclaration(type, method, legacyPredicate))
        .Select(method => DeclarationError(method, InvalidMarkerReason(method)));

    static string InvalidMarkerReason(IMethodSymbol method) => HasSelfParameter(method)
        ? QuerySelectionPolicy.SelfParameterReason
        : "[Query] requires a public/internal static nongeneric ordinary method with value parameters and a supported read-model return shape on [ReadModel]; correct the declaration or remove [Query]";

    static bool HasInvalidDeclaration(INamedTypeSymbol type, IMethodSymbol method, Func<IMethodSymbol, bool> legacyPredicate) =>
        ReadModel(type) is null || !IsValidExplicitQuery(method) || !legacyPredicate(method);

    static IEnumerable<Diagnostic> DuplicateSelectedNames(INamedTypeSymbol type, IEnumerable<IMethodSymbol> methods, Func<IMethodSymbol, bool> legacyPredicate)
    {
        if (!RequiresQueryAttribute(type))
        {
            return [];
        }

        return methods.Where(method => IsMarked(method) && IsValidExplicitQuery(method) && legacyPredicate(method))
            .GroupBy(method => method.Name)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group.Select(method => DeclarationError(
                method,
                "only one selected query per method name is supported; rename a query or remove its [Query] marker")));
    }

    static Diagnostic DeclarationError(IMethodSymbol method, string reason) =>
        Diagnostic.Create(InvalidDeclaration, method.Locations.FirstOrDefault(), method.Name, reason);

    static bool IsOpen(INamedTypeSymbol type) => type.TypeParameters.Length > 0 || (type.ContainingType is { } parent && IsOpen(parent));

    /// <summary>
    /// The exception that is thrown when source tooling encounters invalid explicit query intent.
    /// </summary>
    /// <param name="message">The actionable error.</param>
    public sealed class InvalidQueryDeclaration(string message) : Exception(message);
}
