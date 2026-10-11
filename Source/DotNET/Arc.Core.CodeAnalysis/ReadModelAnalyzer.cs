// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.CodeAnalysis;

/// <summary>
/// Analyzer for ReadModel types.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReadModelAnalyzer : DiagnosticAnalyzer
{
    static readonly string[] _utilitySuffixes = ["Extensions", "Helper", "Helpers", "Builder", "Factory", "Result", "Message"];

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
            DiagnosticDescriptors.ARC0001_IncorrectQueryMethodSignature,
            DiagnosticDescriptors.ARC0014_QueryMethodCannotBeGeneric,
            ModelBoundQueryMethodSymbols.InvalidDeclaration
        ];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        foreach (var diagnostic in ModelBoundQueryMethodSymbols.Validate(type, method => IsValidQueryMethodSignature(method, type)))
        {
            context.ReportDiagnostic(diagnostic);
        }

        if (type.TypeKind != TypeKind.Class || IsUtilityType(type))
        {
            return;
        }

        if (!HasQueryDeclaration(type))
        {
            return;
        }

        ReportIncorrectSignatures(context, type);
        ReportGenericQueries(context, type);
    }

    static bool IsUtilityType(INamedTypeSymbol type) => type.IsStatic ||
        Array.Exists(_utilitySuffixes, suffix => type.Name.EndsWith(suffix, StringComparison.Ordinal));

    static bool HasQueryDeclaration(INamedTypeSymbol type) =>
        HasAttribute(type, "Cratis.Arc.Queries.ModelBound.ReadModelAttribute") || ModelBoundQueryMethodSymbols.RequiresQueryAttribute(type);

    static void ReportIncorrectSignatures(SymbolAnalysisContext context, INamedTypeSymbol type)
    {
        var explicitMode = ModelBoundQueryMethodSymbols.RequiresQueryAttribute(type);
        foreach (var method in FindPotentialQueryMethods(type).Where(method => !HasValidSelectedSignature(method, type, explicitMode)))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ARC0001_IncorrectQueryMethodSignature,
                method.Locations[0],
                method.Name,
                type.Name,
                method.ReturnType.ToDisplayString()));
        }
    }

    static bool HasValidSelectedSignature(IMethodSymbol method, INamedTypeSymbol type, bool explicitMode) => explicitMode
        ? ModelBoundQueryMethodSymbols.IsValidExplicitQuery(method) && IsValidQueryMethodSignature(method, type)
        : IsValidQueryMethodSignature(method, type);

    static void ReportGenericQueries(SymbolAnalysisContext context, INamedTypeSymbol type)
    {
        foreach (var method in FindGenericQueryShapedMethods(type))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ARC0014_QueryMethodCannotBeGeneric,
                method.Locations[0],
                method.Name,
                type.Name));
        }
    }

    static bool HasAttribute(INamedTypeSymbol typeSymbol, string attributeFullName)
    {
        return typeSymbol.GetAttributes().Any(attr =>
            attr.AttributeClass?.ToDisplayString() == attributeFullName);
    }

    static IEnumerable<IMethodSymbol> FindPotentialQueryMethods(INamedTypeSymbol typeSymbol)
    {
        return SelectedStaticMethods(typeSymbol)
            .Where(m => m.DeclaredAccessibility == Accessibility.Public && !m.ReturnsVoid);
    }

    /// <summary>
    /// Finds the methods a read model would have registered as queries had they not been generic.
    /// </summary>
    /// <param name="typeSymbol">The read model to inspect.</param>
    /// <returns>The generic methods carrying a query-shaped return type.</returns>
    /// <remarks>
    /// Deliberately wider than <see cref="FindPotentialQueryMethods"/>, which only looks at public methods: query
    /// discovery registers internal methods too, so an internal generic helper is just as routable — and just as
    /// broken — as a public one.
    /// </remarks>
    static IEnumerable<IMethodSymbol> FindGenericQueryShapedMethods(INamedTypeSymbol typeSymbol)
    {
        return SelectedStaticMethods(typeSymbol)
            .Where(m => m.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal &&
                       m.TypeParameters.Length > 0 && IsValidQueryMethodSignature(m, typeSymbol));
    }

    static IEnumerable<IMethodSymbol> SelectedStaticMethods(INamedTypeSymbol type) => type.GetMembers()
        .OfType<IMethodSymbol>()
        .Where(IsSelectedStaticMethod);

    static bool IsSelectedStaticMethod(IMethodSymbol method) =>
        method is { MethodKind: MethodKind.Ordinary, IsStatic: true } && ModelBoundQueryMethodSymbols.IsSelected(method) &&
        (!ModelBoundQueryMethodSymbols.RequiresQueryAttribute(method.ContainingType) || IsValidQueryMethodSignature(method, method.ContainingType));

    static bool IsValidQueryMethodSignature(IMethodSymbol method, INamedTypeSymbol readModelType)
    {
        var returnType = method.ReturnType;

        if (SymbolEqualityComparer.Default.Equals(returnType, readModelType))
        {
            return true;
        }

        if (returnType is INamedTypeSymbol namedReturnType)
        {
            if (namedReturnType.Name == "Task" &&
                namedReturnType.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks" &&
                namedReturnType.TypeArguments.Length == 1)
            {
                var taskArgumentType = namedReturnType.TypeArguments[0];
                if (SymbolEqualityComparer.Default.Equals(taskArgumentType, readModelType))
                {
                    return true;
                }

                if (IsCollectionOfType(taskArgumentType, readModelType))
                {
                    return true;
                }
            }

            if (IsCollectionOfType(namedReturnType, readModelType))
            {
                return true;
            }

            if (namedReturnType.Name == "IAsyncEnumerable" &&
                namedReturnType.TypeArguments.Length == 1 &&
                SymbolEqualityComparer.Default.Equals(namedReturnType.TypeArguments[0], readModelType))
            {
                return true;
            }

            if (namedReturnType.Name == "ISubject" &&
                namedReturnType.TypeArguments.Length == 1)
            {
                var subjectArgument = namedReturnType.TypeArguments[0];
                if (SymbolEqualityComparer.Default.Equals(subjectArgument, readModelType))
                {
                    return true;
                }

                if (IsCollectionOfType(subjectArgument, readModelType))
                {
                    return true;
                }
            }
        }

        if (returnType is IArrayTypeSymbol arrayType)
        {
            return SymbolEqualityComparer.Default.Equals(arrayType.ElementType, readModelType);
        }

        return false;
    }

    static bool IsCollectionOfType(ITypeSymbol type, ITypeSymbol elementType)
    {
        if (type is IArrayTypeSymbol arrayType)
        {
            return SymbolEqualityComparer.Default.Equals(arrayType.ElementType, elementType);
        }

        if (type is INamedTypeSymbol namedType)
        {
            // Check if the type itself is IEnumerable<T>
            if (namedType.Name == "IEnumerable" &&
                namedType.TypeArguments.Length == 1 &&
                SymbolEqualityComparer.Default.Equals(namedType.TypeArguments[0], elementType))
            {
                return true;
            }

            // Check if the type implements IEnumerable<T>
            var iEnumerableInterface = namedType.AllInterfaces
                .FirstOrDefault(i => i.Name == "IEnumerable" &&
                                    i.TypeArguments.Length == 1 &&
                                    SymbolEqualityComparer.Default.Equals(i.TypeArguments[0], elementType));

            return iEnumerableInterface != null;
        }

        return false;
    }
}
