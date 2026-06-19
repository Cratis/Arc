// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.CodeAnalysis;

/// <summary>
/// Analyzer that ensures concepts deriving from ConceptAs&lt;T&gt; are declared as records.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ConceptRecordAnalyzer : DiagnosticAnalyzer
{
    const string ConceptAsTypeName = "ConceptAs";
    const string ConceptsNamespace = "Cratis.Concepts";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.ARC0009_ConceptShouldBeRecord];

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

        if (namedTypeSymbol.TypeKind != TypeKind.Class || namedTypeSymbol.IsRecord)
        {
            return;
        }

        if (!InheritsFromConceptAs(namedTypeSymbol))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ARC0009_ConceptShouldBeRecord,
            namedTypeSymbol.Locations[0],
            namedTypeSymbol.Name));
    }

    static bool InheritsFromConceptAs(INamedTypeSymbol typeSymbol)
    {
        var current = typeSymbol.BaseType;
        while (current is not null)
        {
            if (current.Name == ConceptAsTypeName &&
                current.ContainingNamespace?.ToDisplayString() == ConceptsNamespace)
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }
}
