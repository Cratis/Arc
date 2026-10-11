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
        QuerySelectionPolicy.IsSelected(RequiresQueryAttribute(method.ContainingType), () => IsMarked(method), () => IsValidExplicitQuery(method));

    /// <summary>
    /// Selects queries with explicit rules or the caller's unchanged legacy predicate.
    /// </summary>
    /// <param name="type">The owner.</param>
    /// <param name="legacyPredicate">The legacy predicate.</param>
    /// <returns>The selected queries.</returns>
    internal static IEnumerable<IMethodSymbol> GetMethods(INamedTypeSymbol type, Func<IMethodSymbol, bool> legacyPredicate) =>
        type.GetMembers().OfType<IMethodSymbol>()
            .Where(method => RequiresQueryAttribute(type) ? IsSelected(method) : legacyPredicate(method));

    /// <summary>
    /// Validates markers before discovery and rejects ambiguous selected names.
    /// </summary>
    /// <param name="type">The owner.</param>
    /// <returns>Actionable declaration errors.</returns>
    internal static IEnumerable<Diagnostic> Validate(INamedTypeSymbol type)
    {
        var methods = type.GetMembers().OfType<IMethodSymbol>().ToArray();
        foreach (var method in methods.Where(IsMarked))
        {
            if (ReadModel(type) is null || !IsValidExplicitQuery(method))
            {
                yield return Diagnostic.Create(
                    InvalidDeclaration,
                    method.Locations.FirstOrDefault(),
                    method.Name,
                    "[Query] requires a public/internal static nongeneric ordinary method with value parameters and a supported read-model return shape on [ReadModel]; correct the declaration or remove [Query]");
            }
        }

        if (RequiresQueryAttribute(type))
        {
            foreach (var duplicate in methods.Where(method => IsMarked(method) && IsValidExplicitQuery(method)).GroupBy(_ => _.Name).Where(_ => _.Count() > 1))
            {
                foreach (var method in duplicate)
                {
                    yield return Diagnostic.Create(
                        InvalidDeclaration,
                        method.Locations.FirstOrDefault(),
                        method.Name,
                        "only one selected query per method name is supported; rename a query or remove its [Query] marker");
                }
            }
        }
    }

    /// <summary>
    /// Rejects invalid intent when a tooling reader cannot emit compiler diagnostics.
    /// </summary>
    /// <param name="type">The owner.</param>
    /// <exception cref="InvalidQueryDeclaration">Explicit query intent is invalid or ambiguous.</exception>
    internal static void EnsureValid(INamedTypeSymbol type)
    {
        var diagnostics = Validate(type).ToArray();
        if (diagnostics.Length > 0)
        {
            throw new InvalidQueryDeclaration(string.Join(" | ", diagnostics.Select(_ => _.GetMessage())));
        }
    }

    /// <summary>
    /// Validates the complete explicit query shape.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <returns>Whether it is eligible in explicit mode.</returns>
    internal static bool IsValidExplicitQuery(IMethodSymbol method) =>
        IsCandidate(method) && !IsOpen(method.ContainingType) && !method.ReturnsByRef && !method.ReturnsByRefReadonly &&
        method.Parameters.All(_ => _.RefKind == RefKind.None) && IsSupportedReturn(method.ReturnType, method.ContainingType);

    static bool IsOpen(INamedTypeSymbol type) => type.TypeParameters.Length > 0 || (type.ContainingType is { } parent && IsOpen(parent));

    static bool IsSupportedReturn(ITypeSymbol type, INamedTypeSymbol owner)
    {
        if (IsGeneric(type, "System.Threading.Tasks.Task`1") && type is INamedTypeSymbol task)
        {
            type = task.TypeArguments[0];
        }

        return SymbolEqualityComparer.Default.Equals(type, owner) || IsCollection(type, owner) ||
            (IsGeneric(type, "System.Collections.Generic.IAsyncEnumerable`1") && type is INamedTypeSymbol stream && SymbolEqualityComparer.Default.Equals(stream.TypeArguments[0], owner)) ||
            (IsGeneric(type, "System.Reactive.Subjects.ISubject`1") && type is INamedTypeSymbol subject &&
                (SymbolEqualityComparer.Default.Equals(subject.TypeArguments[0], owner) || IsCollection(subject.TypeArguments[0], owner)));
    }

    static bool IsCollection(ITypeSymbol type, INamedTypeSymbol owner) =>
        (type is IArrayTypeSymbol array && SymbolEqualityComparer.Default.Equals(array.ElementType, owner)) ||
        (type is INamedTypeSymbol named && new[] { named }.Concat(named.AllInterfaces)
            .Any(_ => IsGeneric(_, "System.Collections.Generic.IEnumerable`1") && SymbolEqualityComparer.Default.Equals(_.TypeArguments[0], owner)));

    static bool IsGeneric(ITypeSymbol type, string name) => type is INamedTypeSymbol named && $"{named.ContainingNamespace.ToDisplayString()}.{named.MetadataName}" == name;

    /// <summary>
    /// The exception that is thrown when source tooling encounters invalid explicit query intent.
    /// </summary>
    /// <param name="message">The actionable error.</param>
    public sealed class InvalidQueryDeclaration(string message) : Exception(message);
}
