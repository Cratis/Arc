// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.CodeAnalysis;

/// <summary>
/// Analyzer for ReadModel types.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReadModelAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
            DiagnosticDescriptors.ARC0001_IncorrectQueryMethodSignature
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
        var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

        if (namedTypeSymbol.TypeKind != TypeKind.Class)
        {
            return;
        }

        // Skip extension classes, utility classes, and types that are clearly not ReadModels
        if (namedTypeSymbol.Name.EndsWith("Extensions", StringComparison.Ordinal) ||
            namedTypeSymbol.Name.EndsWith("Helper", StringComparison.Ordinal) ||
            namedTypeSymbol.Name.EndsWith("Helpers", StringComparison.Ordinal) ||
            namedTypeSymbol.Name.EndsWith("Builder", StringComparison.Ordinal) ||
            namedTypeSymbol.Name.EndsWith("Factory", StringComparison.Ordinal) ||
            namedTypeSymbol.Name.EndsWith("Result", StringComparison.Ordinal) ||
            namedTypeSymbol.Name.EndsWith("Message", StringComparison.Ordinal) ||
            namedTypeSymbol.IsStatic)
        {
            return;
        }

        var hasReadModelAttribute = HasAttribute(namedTypeSymbol, "Cratis.Arc.Queries.ModelBound.ReadModelAttribute");
        var queryMethods = FindPotentialQueryMethods(namedTypeSymbol);

        if (hasReadModelAttribute)
        {
            foreach (var method in queryMethods)
            {
                if (!IsValidQueryMethodSignature(method, namedTypeSymbol))
                {
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.ARC0001_IncorrectQueryMethodSignature,
                        method.Locations[0],
                        method.Name,
                        namedTypeSymbol.Name,
                        method.ReturnType.ToDisplayString());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    static bool HasAttribute(INamedTypeSymbol typeSymbol, string attributeFullName)
    {
        return typeSymbol.GetAttributes().Any(attr =>
            attr.AttributeClass?.ToDisplayString() == attributeFullName);
    }

    static IEnumerable<IMethodSymbol> FindPotentialQueryMethods(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Ordinary &&
                       m.IsStatic &&
                       m.DeclaredAccessibility == Accessibility.Public &&
                       !m.ReturnsVoid);
    }

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
            var iEnumerableInterface = namedType.AllInterfaces
                .FirstOrDefault(i => i.Name == "IEnumerable" &&
                                    i.TypeArguments.Length == 1 &&
                                    SymbolEqualityComparer.Default.Equals(i.TypeArguments[0], elementType));

            return iEnumerableInterface != null;
        }

        return false;
    }
}
