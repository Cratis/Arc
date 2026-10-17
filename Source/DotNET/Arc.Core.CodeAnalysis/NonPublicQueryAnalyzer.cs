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
        if (!IsNonPublicStatic(method))
        {
            return;
        }

        if (!IsOnReadModel(method) || !LooksLikeQuery(method))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ARC0029_NonPublicQuery,
            method.Locations[0],
            method.Name,
            method.ContainingType.Name));
    }

    static bool IsNonPublicStatic(IMethodSymbol method) =>
        method is { MethodKind: MethodKind.Ordinary, IsStatic: true } && method.DeclaredAccessibility != Accessibility.Public;

    static bool IsOnReadModel(IMethodSymbol method) => ModelBoundQueryMethodSymbols.ReadModel(method.ContainingType) is not null;

    static bool LooksLikeQuery(IMethodSymbol method) =>
        ModelBoundQueryMethodSymbols.IsMarked(method) || ModelBoundQueryReturnTypes.IsSupported(method.ReturnType, method.ContainingType);
}
