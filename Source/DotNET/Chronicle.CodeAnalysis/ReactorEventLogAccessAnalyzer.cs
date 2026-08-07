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
                    parameter.Name));
            }
        }
    }

    static void AnalyzeEventLogProperty(SyntaxNodeAnalysisContext context)
    {
        var eventLogAccess = (ExpressionSyntax)context.Node;

        if (AccessedName(eventLogAccess)?.Identifier.ValueText != EventLogPropertyName ||
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
        if (!IsAppendedTo(context.SemanticModel, eventLogAccess) || !IsTheReactorsOwnEventStore(context.SemanticModel, eventLogAccess))
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
            Describe(eventLogAccess)));
    }

    /// <summary>
    /// Determines whether the sequence is appended to, following the chain past members that hand back the
    /// same sequence.
    /// </summary>
    /// <param name="semanticModel">The <see cref="SemanticModel"/> to resolve symbols with.</param>
    /// <param name="eventLogAccess">The event log or event sequence access to inspect.</param>
    /// <returns>True if the sequence is appended to, false otherwise.</returns>
    /// <remarks>
    /// <c>Transactional</c> hands back the very same sequence enlisted in a unit of work, so
    /// <c>EventLog.Transactional.Append(...)</c> is the identical write with one more member in the chain — the
    /// shape Chronicle steers authors toward, and the one this rule has to see.
    /// </remarks>
    static bool IsAppendedTo(SemanticModel semanticModel, ExpressionSyntax eventLogAccess)
    {
        var current = eventLogAccess;

        while (NextAccessOn(current) is { } next)
        {
            if (AccessedName(next)!.Identifier.ValueText.StartsWith(AppendMethodPrefix, StringComparison.Ordinal))
            {
                return next.Parent is InvocationExpressionSyntax;
            }

            if (!IsTransactionalEventSequence(semanticModel, next))
            {
                return false;
            }

            current = next;
        }

        return false;
    }

    static bool IsTransactionalEventSequence(SemanticModel semanticModel, ExpressionSyntax access) =>
        AccessedName(access)?.Identifier.ValueText == TransactionalPropertyName &&
        semanticModel.GetSymbolInfo(access).Symbol is IPropertySymbol property &&
        IsEventSequence(property.ContainingType);

    static ExpressionSyntax? NextAccessOn(ExpressionSyntax expression) => expression.Parent switch
    {
        MemberAccessExpressionSyntax member when member.Expression == expression => member,
        ConditionalAccessExpressionSyntax conditional when conditional.Expression == expression => LeadingBindingOf(conditional.WhenNotNull),
        _ => null
    };

    static MemberBindingExpressionSyntax? LeadingBindingOf(ExpressionSyntax expression) => expression switch
    {
        MemberBindingExpressionSyntax binding => binding,
        MemberAccessExpressionSyntax member => LeadingBindingOf(member.Expression),
        InvocationExpressionSyntax invocation => LeadingBindingOf(invocation.Expression),
        ConditionalAccessExpressionSyntax conditional => LeadingBindingOf(conditional.Expression),
        _ => null
    };

    static SimpleNameSyntax? AccessedName(ExpressionSyntax expression) => expression switch
    {
        MemberAccessExpressionSyntax member => member.Name,
        MemberBindingExpressionSyntax binding => binding.Name,
        _ => null
    };

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

    static ExpressionSyntax? EventStoreExpression(ExpressionSyntax eventLogAccess) => AccessorOf(eventLogAccess) switch
    {
        MemberAccessExpressionSyntax member => Unwrap(member.Expression),
        MemberBindingExpressionSyntax binding => ConditionalReceiverOf(binding),
        _ => null
    };

    static ExpressionSyntax AccessorOf(ExpressionSyntax eventLogAccess) =>
        eventLogAccess is InvocationExpressionSyntax invocation ? invocation.Expression : eventLogAccess;

    static ExpressionSyntax? ConditionalReceiverOf(MemberBindingExpressionSyntax binding) =>
        binding.Ancestors().OfType<ConditionalAccessExpressionSyntax>().FirstOrDefault() is { } conditional
            ? Unwrap(conditional.Expression)
            : null;

    static string Describe(ExpressionSyntax eventLogAccess) =>
        AccessorOf(eventLogAccess) is MemberBindingExpressionSyntax binding && ConditionalReceiverOf(binding) is { } receiver
            ? $"{receiver}?{eventLogAccess}"
            : eventLogAccess.ToString();

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
