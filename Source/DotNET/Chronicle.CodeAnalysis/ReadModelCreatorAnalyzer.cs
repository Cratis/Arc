// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Queries.ModelBound;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Warns when a conventional query resembles a read-model fold.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReadModelCreatorAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.ARCCHR0019_ReadModelFoldMayBeQuery];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    /// <summary>
    /// Recognizes possible fold shapes without using the runtime event universe or excluding queries.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <returns>Whether the method resembles a fold.</returns>
    internal static bool IsPossibleFold(IMethodSymbol method)
    {
        var result = method.ReturnType;
        if (result is INamedTypeSymbol { TypeArguments.Length: 1 } task && task.Name == "Task" && task.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks")
        {
            result = task.TypeArguments[0];
        }

        if (!ModelBoundQueryMethodSymbols.IsCandidate(method) ||
            !SymbolEqualityComparer.Default.Equals(result, method.ContainingType) ||
            method.Parameters.Length == 0 || !IsPossibleEvent(method.Parameters[0].Type))
        {
            return false;
        }

        return (method.Name == "Create" && method.Parameters.Length is 1 or 2) ||
            (method.Name == "Reduce" && method.Parameters.Length is 2 or 3 &&
                SymbolEqualityComparer.Default.Equals(method.Parameters[1].Type, method.ContainingType) &&
                method.Parameters[1].NullableAnnotation == NullableAnnotation.Annotated &&
                (method.Parameters.Length == 2 || method.Parameters[2].Type.ToDisplayString().TrimEnd('?') == "Cratis.Chronicle.Events.EventContext"));
    }

    static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (ModelBoundQueryMethodSymbols.ReadModel(method.ContainingType) is null ||
            ModelBoundQueryMethodSymbols.RequiresQueryAttribute(method.ContainingType) || !IsPossibleFold(method))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ARCCHR0019_ReadModelFoldMayBeQuery,
            method.Locations[0],
            method.ContainingType.Name,
            method.Name));
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
