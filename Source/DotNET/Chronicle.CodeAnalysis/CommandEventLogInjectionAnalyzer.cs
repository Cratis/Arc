// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Analyzer that warns when a command injects <c>IEventLog</c> into its handler method instead of expressing appends through the return type.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CommandEventLogInjectionAnalyzer : DiagnosticAnalyzer
{
    const string CommandAttributeName = "Cratis.Arc.Commands.ModelBound.CommandAttribute";
    const string EventLogInterfaceName = "IEventLog";
    const string EventSequencesNamespace = "Cratis.Chronicle.EventSequences";
    const string ReturnEventsAdvice = "Express every append through the handler return type, not IEventLog";
    const string ReturnCompleteStreamAdvice = "Express every append through the handler return type, and close a stream by returning CompleteStream (see the completing streams documentation), not by calling IEventLog.CompleteStream";
    static readonly string[] _handlerMethodNames = ["Handle", "Provide"];

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.ARCCHR0007_CommandHandleMustNotInjectEventLog];

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

        if (namedTypeSymbol.TypeKind != TypeKind.Class || !IsCommand(namedTypeSymbol))
        {
            return;
        }

        var handlerMethods = namedTypeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(method =>
                method.MethodKind == MethodKind.Ordinary &&
                _handlerMethodNames.Contains(method.Name));

        foreach (var method in handlerMethods)
        {
            foreach (var parameter in method.Parameters.Where(parameter => IsEventLog(parameter.Type)))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ARCCHR0007_CommandHandleMustNotInjectEventLog,
                    parameter.Locations[0],
                    namedTypeSymbol.Name,
                    method.Name,
                    parameter.Name,
                    CompletesStream(context, method, parameter) ? ReturnCompleteStreamAdvice : ReturnEventsAdvice));
            }
        }
    }

    static bool CompletesStream(SymbolAnalysisContext context, IMethodSymbol method, IParameterSymbol parameter) =>
        method.DeclaringSyntaxReferences.Any(reference =>
            reference.GetSyntax(context.CancellationToken).DescendantNodes().OfType<MemberAccessExpressionSyntax>().Any(access =>
                access.Name.Identifier.ValueText == "CompleteStream" &&
                access.Expression is IdentifierNameSyntax receiver &&
                receiver.Identifier.ValueText == parameter.Name));

    static bool IsCommand(INamedTypeSymbol typeSymbol) =>
        typeSymbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == CommandAttributeName);

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
