// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Analyzer for AggregateRoot types.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AggregateRootAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.ARCCHR0001_IncorrectAggregateRootEventHandlerSignature];

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

        if (!IsAggregateRoot(namedTypeSymbol))
        {
            return;
        }

        foreach (var method in FindEventHandlerMethods(namedTypeSymbol))
        {
            if (!IsValidEventHandlerSignature(method))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ARCCHR0001_IncorrectAggregateRootEventHandlerSignature,
                    method.Locations[0],
                    method.Name,
                    namedTypeSymbol.Name,
                    GetMethodSignature(method)));
            }
        }
    }

    static bool IsAggregateRoot(INamedTypeSymbol typeSymbol)
    {
        var currentType = typeSymbol.BaseType;
        while (currentType != null)
        {
            if (currentType.Name == "AggregateRoot" &&
                currentType.ContainingNamespace.ToDisplayString() == "Cratis.Arc.Chronicle.Aggregates")
            {
                return true;
            }
            currentType = currentType.BaseType;
        }

        var interfaces = typeSymbol.AllInterfaces;
        return interfaces.Any(i =>
            i.Name == "IAggregateRoot" &&
            i.ContainingNamespace.ToDisplayString() == "Cratis.Arc.Chronicle.Aggregates");
    }

    static IEnumerable<IMethodSymbol> FindEventHandlerMethods(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Ordinary &&
                       m.Name.StartsWith("On", StringComparison.Ordinal) &&
                       m.Parameters.Length > 0 &&
                       m.Parameters.Length <= 2);
    }

    static bool IsValidEventHandlerSignature(IMethodSymbol method)
    {
        var returnType = method.ReturnType;
        var isVoidOrTask = returnType.SpecialType == SpecialType.System_Void ||
                          (returnType.Name == "Task" &&
                           returnType.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks" &&
                           returnType is INamedTypeSymbol namedReturnType &&
                           namedReturnType.TypeArguments.Length == 0);

        if (!isVoidOrTask)
        {
            return false;
        }

        var parameters = method.Parameters;

        if (parameters.Length == 1)
        {
            return true;
        }

        if (parameters.Length == 2)
        {
            var secondParam = parameters[1];
            return secondParam.Type.Name == "EventContext" &&
                   secondParam.Type.ContainingNamespace.ToDisplayString() == "Cratis.Chronicle.Events";
        }

        return false;
    }

    static string GetMethodSignature(IMethodSymbol method)
    {
        var parameters = string.Join(", ", method.Parameters.Select(p => p.Type.ToDisplayString()));
        return $"{method.ReturnType.ToDisplayString()} {method.Name}({parameters})";
    }
}
