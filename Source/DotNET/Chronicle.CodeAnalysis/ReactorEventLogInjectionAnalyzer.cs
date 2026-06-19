// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Analyzer that warns when a reactor injects <c>IEventLog</c> instead of returning side-effect events.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReactorEventLogInjectionAnalyzer : DiagnosticAnalyzer
{
    const string ReactorInterfaceName = "IReactor";
    const string ReactorsNamespace = "Cratis.Chronicle.Reactors";
    const string EventLogInterfaceName = "IEventLog";
    const string EventSequencesNamespace = "Cratis.Chronicle.EventSequences";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.ARCCHR0003_ReactorMustNotInjectEventLog];

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

        if (namedTypeSymbol.TypeKind != TypeKind.Class || !IsReactor(namedTypeSymbol))
        {
            return;
        }

        foreach (var constructor in namedTypeSymbol.InstanceConstructors)
        {
            foreach (var parameter in constructor.Parameters.Where(parameter => IsEventLog(parameter.Type)))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ARCCHR0003_ReactorMustNotInjectEventLog,
                    parameter.Locations[0],
                    namedTypeSymbol.Name,
                    parameter.Name));
            }
        }
    }

    static bool IsReactor(INamedTypeSymbol typeSymbol) =>
        typeSymbol.AllInterfaces.Any(@interface =>
            @interface.Name == ReactorInterfaceName &&
            @interface.ContainingNamespace?.ToDisplayString() == ReactorsNamespace);

    static bool IsEventLog(ITypeSymbol type)
    {
        if (type.Name == EventLogInterfaceName &&
            type.ContainingNamespace?.ToDisplayString() == EventSequencesNamespace)
        {
            return true;
        }

        return type.AllInterfaces.Any(@interface =>
            @interface.Name == EventLogInterfaceName &&
            @interface.ContainingNamespace?.ToDisplayString() == EventSequencesNamespace);
    }
}
