// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Warns about possible fold creators whose event parameter cannot be recognized by query discovery.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReadModelCreatorAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.ARCCHR0019_ReadModelCreatorNeedsNotAQuery];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (!ModelBoundQueryMethodSymbols.IsCandidate(method) ||
            !ModelBoundQueryMethodSymbols.IsCreator(method) ||
            !method.ContainingType.GetAttributes().Any(_ => _.AttributeClass?.ToDisplayString() == "Cratis.Arc.Queries.ModelBound.ReadModelAttribute") ||
            !IsPossibleEvent(method.Parameters[0].Type))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ARCCHR0019_ReadModelCreatorNeedsNotAQuery,
            method.Locations[0],
            method.ContainingType.Name));
    }

    static bool IsPossibleEvent(ITypeSymbol type)
    {
        if (type.TypeKind is not (TypeKind.Class or TypeKind.Interface) || type.SpecialType == SpecialType.System_String)
        {
            return false;
        }

        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.Name == "ConceptAs" && current.ContainingNamespace.ToDisplayString() == "Cratis.Concepts")
            {
                return false;
            }
        }

        return true;
    }
}
