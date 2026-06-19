// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.CodeAnalysis;

/// <summary>
/// Analyzer that ensures model-bound command and read model types are declared as records.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ModelBoundRecordAnalyzer : DiagnosticAnalyzer
{
    const string CommandAttributeName = "Cratis.Arc.Commands.ModelBound.CommandAttribute";
    const string ReadModelAttributeName = "Cratis.Arc.Queries.ModelBound.ReadModelAttribute";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        DiagnosticDescriptors.ARC0007_CommandShouldBeRecord,
        DiagnosticDescriptors.ARC0008_ReadModelShouldBeRecord
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

        if (namedTypeSymbol.IsRecord)
        {
            return;
        }

        if (HasAttribute(namedTypeSymbol, CommandAttributeName))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ARC0007_CommandShouldBeRecord,
                namedTypeSymbol.Locations[0],
                namedTypeSymbol.Name));
        }
        else if (HasAttribute(namedTypeSymbol, ReadModelAttributeName))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ARC0008_ReadModelShouldBeRecord,
                namedTypeSymbol.Locations[0],
                namedTypeSymbol.Name));
        }
    }

    static bool HasAttribute(INamedTypeSymbol typeSymbol, string attributeFullName) =>
        typeSymbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == attributeFullName);
}
