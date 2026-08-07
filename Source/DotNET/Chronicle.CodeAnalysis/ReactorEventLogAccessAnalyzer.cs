// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Analyzer that warns when a reactor reaches the default event log instead of returning side-effect events.
/// </summary>
/// <remarks>
/// Two shapes reach the same sequence: injecting <c>IEventLog</c>, and appending through an injected
/// <c>IEventStore</c> — either its <c>EventLog</c> property or <c>GetEventSequence(EventSequenceId.Log)</c>.
/// Routing to any other sequence through <c>GetEventSequence</c> — the outbox in particular — is not
/// reported, because a returned event cannot target a sequence other than the default log.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReactorEventLogAccessAnalyzer : DiagnosticAnalyzer
{
    const string ReactorInterfaceName = "IReactor";
    const string ReactorsNamespace = "Cratis.Chronicle.Reactors";
    const string EventLogInterfaceName = "IEventLog";
    const string EventSequencesNamespace = "Cratis.Chronicle.EventSequences";
    const string EventStoreInterfaceName = "IEventStore";
    const string ChronicleNamespace = "Cratis.Chronicle";
    const string EventLogPropertyName = "EventLog";
    const string GetEventSequenceMethodName = "GetEventSequence";
    const string EventSequenceIdTypeName = "EventSequenceId";
    const string DefaultEventLogFieldName = "Log";
    const string DefaultEventLogId = "event-log";
    const string AppendMethodPrefix = "Append";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.ARCCHR0003_ReactorMustNotReachEventLog];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        context.RegisterSyntaxNodeAction(AnalyzeEventLogProperty, SyntaxKind.SimpleMemberAccessExpression);
        context.RegisterSyntaxNodeAction(AnalyzeGetEventSequence, SyntaxKind.InvocationExpression);
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
                    DiagnosticDescriptors.ARCCHR0003_ReactorMustNotReachEventLog,
                    parameter.Locations[0],
                    namedTypeSymbol.Name,
                    parameter.Name));
            }
        }
    }

    static void AnalyzeEventLogProperty(SyntaxNodeAnalysisContext context)
    {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;

        if (memberAccess.Name.Identifier.ValueText != EventLogPropertyName ||
            context.SemanticModel.GetSymbolInfo(memberAccess).Symbol is not IPropertySymbol property ||
            !IsEventStore(property.ContainingType))
        {
            return;
        }

        ReportWhenAppendingInsideReactor(context, memberAccess);
    }

    static void AnalyzeGetEventSequence(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method ||
            method.Name != GetEventSequenceMethodName ||
            !IsEventStore(method.ContainingType) ||
            invocation.ArgumentList.Arguments.Count != 1 ||
            !IsDefaultEventLog(context.SemanticModel, invocation.ArgumentList.Arguments[0].Expression))
        {
            return;
        }

        ReportWhenAppendingInsideReactor(context, invocation);
    }

    static void ReportWhenAppendingInsideReactor(SyntaxNodeAnalysisContext context, ExpressionSyntax eventLogAccess)
    {
        if (!IsAppendedTo(eventLogAccess))
        {
            return;
        }

        var containingType = context.ContainingSymbol?.ContainingType;
        if (containingType is null || !IsReactor(containingType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ARCCHR0003_ReactorMustNotReachEventLog,
            eventLogAccess.GetLocation(),
            containingType.Name,
            eventLogAccess.ToString()));
    }

    static bool IsAppendedTo(ExpressionSyntax eventLogAccess) =>
        eventLogAccess.Parent is MemberAccessExpressionSyntax memberAccess &&
        memberAccess.Expression == eventLogAccess &&
        memberAccess.Name.Identifier.ValueText.StartsWith(AppendMethodPrefix, StringComparison.Ordinal) &&
        memberAccess.Parent is InvocationExpressionSyntax;

    static bool IsDefaultEventLog(SemanticModel semanticModel, ExpressionSyntax expression)
    {
        var constant = semanticModel.GetConstantValue(expression);
        if (constant.HasValue)
        {
            return (constant.Value as string) == DefaultEventLogId;
        }

        return semanticModel.GetSymbolInfo(expression).Symbol is IFieldSymbol field &&
            field.Name == DefaultEventLogFieldName &&
            field.ContainingType?.Name == EventSequenceIdTypeName &&
            field.ContainingType?.ContainingNamespace?.ToDisplayString() == EventSequencesNamespace;
    }

    static bool IsReactor(INamedTypeSymbol typeSymbol) =>
        typeSymbol.AllInterfaces.Any(@interface =>
            @interface.Name == ReactorInterfaceName &&
            @interface.ContainingNamespace?.ToDisplayString() == ReactorsNamespace);

    static bool IsEventLog(ITypeSymbol type) =>
        IsInterface(type, EventLogInterfaceName, EventSequencesNamespace) ||
        type.AllInterfaces.Any(@interface => IsInterface(@interface, EventLogInterfaceName, EventSequencesNamespace));

    static bool IsEventStore(ITypeSymbol? type) =>
        type is not null &&
        (IsInterface(type, EventStoreInterfaceName, ChronicleNamespace) ||
         type.AllInterfaces.Any(@interface => IsInterface(@interface, EventStoreInterfaceName, ChronicleNamespace)));

    static bool IsInterface(ITypeSymbol type, string name, string containingNamespace) =>
        type.Name == name &&
        type.ContainingNamespace?.ToDisplayString() == containingNamespace;
}
