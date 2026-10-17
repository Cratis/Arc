// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.CodeAnalysis;

/// <summary>
/// Explains why non-public static methods on read models are not exposed as queries.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NonPublicQueryAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.ARC0029_NonPublicQuery];

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
        if (method is not { MethodKind: MethodKind.Ordinary, IsStatic: true } ||
            method.DeclaredAccessibility == Accessibility.Public ||
            ModelBoundQueryMethodSymbols.ReadModel(method.ContainingType) is null ||
            (!ModelBoundQueryMethodSymbols.IsMarked(method) && !ModelBoundQueryReturnTypes.IsSupported(method.ReturnType, method.ContainingType)))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ARC0029_NonPublicQuery,
            method.Locations[0],
            method.Name,
            method.ContainingType.Name));
    }
}
