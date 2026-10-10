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
    const string EventSequenceInterfaceName = "IEventSequence";
    const string EventSequencesNamespace = "Cratis.Chronicle.EventSequences";
    const string EventStoreInterfaceName = "IEventStore";
    const string ChronicleNamespace = "Cratis.Chronicle";
    const string EventLogPropertyName = "EventLog";
    const string GetEventSequenceMethodName = "GetEventSequence";
    const string EventSequenceIdTypeName = "EventSequenceId";
    const string DefaultEventLogFieldName = "Log";
    const string DefaultEventLogId = "event-log";
    const string AppendMethodPrefix = "Append";
    const string TransactionalPropertyName = "Transactional";
    const string CompleteStreamMethodName = "CompleteStream";
    const string ReturnEventsAdvice = "Return the events from the handler method — a single event, an IEnumerable<object>, or EventForEventSourceId wrappers — instead of appending directly";
    const string ReturnCompleteStreamAdvice = "Return CompleteStream from the handler method to close the stream instead of calling CompleteStream on the event log (see the completing streams documentation)";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.ARCCHR0003_ReactorMustNotReachEventLog];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        context.RegisterSyntaxNodeAction(AnalyzeEventLogProperty, SyntaxKind.SimpleMemberAccessExpression, SyntaxKind.MemberBindingExpression);
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
                    parameter.Name,
                    ReturnEventsAdvice));
            }
        }
    }

    static void AnalyzeEventLogProperty(SyntaxNodeAnalysisContext context)
    {
        var eventLogAccess = (ExpressionSyntax)context.Node;

        if (MemberAccessChain.NameOf(eventLogAccess)?.Identifier.ValueText != EventLogPropertyName ||
            context.SemanticModel.GetSymbolInfo(eventLogAccess).Symbol is not IPropertySymbol property ||
            !IsEventStore(property.ContainingType))
        {
            return;
        }

        ReportWhenAppendingInsideReactor(context, eventLogAccess);
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
        var member = WrittenWith(context.SemanticModel, eventLogAccess);
        if (member is null || !IsTheReactorsOwnEventStore(context.SemanticModel, eventLogAccess))
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
            MemberAccessChain.Describe(eventLogAccess),
            member == CompleteStreamMethodName ? ReturnCompleteStreamAdvice : ReturnEventsAdvice));
    }

    /// <summary>
    /// Determines whether the sequence is written to — appended to or its stream completed — following the chain
    /// past members that hand back the same sequence.
    /// </summary>
    /// <param name="semanticModel">The <see cref="SemanticModel"/> to resolve symbols with.</param>
    /// <param name="eventLogAccess">The event log or event sequence access to inspect.</param>
    /// <returns>The written-with member kind: the append method name prefix, <c>CompleteStream</c>, or null when the sequence is only read.</returns>
    /// <remarks>
    /// <c>Transactional</c> hands back the very same sequence enlisted in a unit of work, so
    /// <c>EventLog.Transactional.Append(...)</c> is the identical write with one more member in the chain — the
    /// shape Chronicle steers authors toward, and the one this rule has to see.
    /// </remarks>
    static string? WrittenWith(SemanticModel semanticModel, ExpressionSyntax eventLogAccess)
    {
        var current = eventLogAccess;

        while (MemberAccessChain.Next(current) is { } next)
        {
            var name = MemberAccessChain.NameOf(next)!.Identifier.ValueText;
            if (name.StartsWith(AppendMethodPrefix, StringComparison.Ordinal))
            {
                return next.Parent is InvocationExpressionSyntax ? AppendMethodPrefix : null;
            }

            if (name == CompleteStreamMethodName)
            {
                return next.Parent is InvocationExpressionSyntax ? CompleteStreamMethodName : null;
            }

            if (!IsTransactionalEventSequence(semanticModel, next))
            {
                return null;
            }

            current = next;
        }

        return null;
    }

    static bool IsTransactionalEventSequence(SemanticModel semanticModel, ExpressionSyntax access) =>
        MemberAccessChain.NameOf(access)?.Identifier.ValueText == TransactionalPropertyName &&
        semanticModel.GetSymbolInfo(access).Symbol is IPropertySymbol property &&
        IsEventSequence(property.ContainingType);

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
        MemberAccessChain.ReceiverOf(eventLogAccess) is { } eventStore &&
        semanticModel.GetSymbolInfo(eventStore).Symbol is IParameterSymbol or IFieldSymbol or IPropertySymbol;

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

    static bool IsEventSequence(ITypeSymbol? type) =>
        type is not null &&
        (IsInterface(type, EventSequenceInterfaceName, EventSequencesNamespace) ||
         type.AllInterfaces.Any(@interface => IsInterface(@interface, EventSequenceInterfaceName, EventSequencesNamespace)));

    static bool IsEventStore(ITypeSymbol? type) =>
        type is not null &&
        (IsInterface(type, EventStoreInterfaceName, ChronicleNamespace) ||
         type.AllInterfaces.Any(@interface => IsInterface(@interface, EventStoreInterfaceName, ChronicleNamespace)));

    static bool IsInterface(ITypeSymbol type, string name, string containingNamespace) =>
        type.Name == name &&
        type.ContainingNamespace?.ToDisplayString() == containingNamespace;
}
