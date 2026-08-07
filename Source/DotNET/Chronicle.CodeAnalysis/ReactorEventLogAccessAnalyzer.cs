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
/// Two shapes are deliberately left alone, because a returned side-effect event cannot express either of them:
/// routing to another sequence through <c>GetEventSequence</c> — the outbox in particular — and appending to an
/// event store other than the one the reactor was handed, such as one obtained from
/// <c>IChronicleClient.GetEventStore</c>.
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
        if (!IsAppendedTo(eventLogAccess) || !IsTheReactorsOwnEventStore(context.SemanticModel, eventLogAccess))
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

    /// <summary>
    /// Determines whether the sequence is reached through the event store the reactor was handed.
    /// </summary>
    /// <param name="semanticModel">The <see cref="SemanticModel"/> to resolve symbols with.</param>
    /// <param name="eventLogAccess">The event log or event sequence access to inspect.</param>
    /// <returns>True if the store is one the reactor holds, false otherwise.</returns>
    /// <remarks>
    /// The rule's advice — return the events instead — only appends to the reactor's own store's default log.
    /// A store the reactor obtained at runtime, from <c>IChronicleClient.GetEventStore</c>, is a different store
    /// in a namespace of its own that no returned event can reach, so following the advice there would write to
    /// the wrong place. Only a store held as a parameter, field, or property counts as the reactor's own.
    /// </remarks>
    static bool IsTheReactorsOwnEventStore(SemanticModel semanticModel, ExpressionSyntax eventLogAccess) =>
        EventStoreExpression(eventLogAccess) is { } eventStore &&
        semanticModel.GetSymbolInfo(eventStore).Symbol is IParameterSymbol or IFieldSymbol or IPropertySymbol;

    static ExpressionSyntax? EventStoreExpression(ExpressionSyntax eventLogAccess)
    {
        var accessor = eventLogAccess is InvocationExpressionSyntax invocation ? invocation.Expression : eventLogAccess;

        return accessor is MemberAccessExpressionSyntax member ? Unwrap(member.Expression) : null;
    }

    static ExpressionSyntax Unwrap(ExpressionSyntax expression) => expression switch
    {
        ParenthesizedExpressionSyntax parenthesized => Unwrap(parenthesized.Expression),
        PostfixUnaryExpressionSyntax postfix when postfix.IsKind(SyntaxKind.SuppressNullableWarningExpression) => Unwrap(postfix.Operand),
        _ => expression
    };

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
