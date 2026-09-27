// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>Advises on recognizable unguarded Chronicle reads and immediate appends in event-producing commands.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CommandDecisionReadAnalyzer : DiagnosticAnalyzer
{
    const string CommandAttribute = "Cratis.Arc.Commands.ModelBound.CommandAttribute";
    const string UnprotectedAttribute = "Cratis.Arc.Chronicle.ReadModels.UnprotectedAttribute";
    const string ChronicleReadModels = "Cratis.Chronicle.ReadModels.IReadModels";
    const string DecisionReads = "Cratis.Chronicle.ReadModels.IDecisionReads";
    const string EventLog = "Cratis.Chronicle.EventSequences.IEventLog";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        DiagnosticDescriptors.ARCCHR0011_UnprotectedDecisionRead,
        DiagnosticDescriptors.ARCCHR0012_ImmediateAppendAfterDecisionRead
    ];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeParameter, SyntaxKind.Parameter);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    static void AnalyzeParameter(SyntaxNodeAnalysisContext context)
    {
        var syntax = (ParameterSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(syntax, context.CancellationToken) is not IParameterSymbol parameter ||
            parameter.ContainingSymbol is not IMethodSymbol method || !InDecision(method, out var command) ||
            HasUnprotected(command) || HasUnprotected(method) || HasUnprotected(parameter) ||
            IsDecision(parameter.Type) || !IsChronicleBacked(parameter.Type, context.Compilation))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ARCCHR0011_UnprotectedDecisionRead,
            syntax.GetLocation(),
            command.Name,
            parameter.Type.Name));
    }

    static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var syntax = (InvocationExpressionSyntax)context.Node;
        if (context.SemanticModel.GetEnclosingSymbol(syntax.SpanStart, context.CancellationToken) is not IMethodSymbol enclosing ||
            !InDecision(enclosing, out var command) || HasUnprotected(command) ||
            context.SemanticModel.GetSymbolInfo(syntax, context.CancellationToken).Symbol is not IMethodSymbol called)
        {
            return;
        }

        if (called.Name == "GetInstanceById" && Implements(called.ContainingType, ChronicleReadModels) && !HasUnprotected(enclosing))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ARCCHR0011_UnprotectedDecisionRead,
                syntax.GetLocation(),
                command.Name,
                called.TypeArguments.FirstOrDefault()?.Name ?? "read model"));
        }

        if (!IsImmediateAppend(syntax, called, context.SemanticModel, context.CancellationToken))
        {
            return;
        }
        var methods = command.GetMembers().OfType<IMethodSymbol>().Where(_ => _.Name == "Handle" || _.Name == "Provide");
        var usesDecision = methods.Any(_ => _.Parameters.Any(p => IsDecision(p.Type))) ||
            enclosing.ContainingType.GetMembers().OfType<IMethodSymbol>().Any(_ => _.Parameters.Any(p => IsDecision(p.Type))) ||
            (enclosing.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(context.CancellationToken) is { } enclosingSyntax &&
             enclosingSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(_ =>
                 context.SemanticModel.GetSymbolInfo(_, context.CancellationToken).Symbol is IMethodSymbol invoked &&
                 Implements(invoked.ContainingType, DecisionReads)));
        if (usesDecision)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ARCCHR0012_ImmediateAppendAfterDecisionRead,
                syntax.GetLocation(),
                command.Name));
        }
    }

    static bool InDecision(IMethodSymbol method, out INamedTypeSymbol command)
    {
        command = method.ContainingType;
        if (IsCommand(command))
        {
            return method.Name == "Handle" || method.Name == "Provide";
        }

        for (var current = command.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.Name == "CommandValidator" &&
                current.ContainingNamespace.ToDisplayString() == "Cratis.Arc.Commands" &&
                current.TypeArguments[0] is INamedTypeSymbol candidate && IsCommand(candidate))
            {
                command = candidate;
                return true;
            }
        }
        return false;
    }

    static bool IsCommand(INamedTypeSymbol type) => type.GetAttributes().Any(_ => _.AttributeClass?.ToDisplayString() == CommandAttribute) &&
        type.GetMembers("Handle").OfType<IMethodSymbol>().Any(_ => ProducesEvents(_.ReturnType) || _.Parameters.Any(p => IsAggregate(p.Type)));

    static bool HasUnprotected(ISymbol symbol) => symbol.GetAttributes().Any(_ => _.AttributeClass?.ToDisplayString() == UnprotectedAttribute);

    static bool IsDecision(ITypeSymbol type) =>
        type.OriginalDefinition.ToDisplayString() == "Cratis.Chronicle.ReadModels.DecisionRead<T>" || Implements(type, DecisionReads);

    static bool IsAggregate(ITypeSymbol type)
    {
        for (var current = type as INamedTypeSymbol; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString() == "Cratis.Arc.Chronicle.Aggregates.AggregateRoot")
            {
                return true;
            }
        }
        return false;
    }

    static bool ProducesEvents(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { IsGenericType: true } named && (named.Name == "Task" || named.Name == "ValueTask" || named.Name == "Result"))
        {
            type = named.TypeArguments[0];
        }
        if (type is INamedTypeSymbol tuple && tuple.IsTupleType)
        {
            return tuple.TupleElements.Any(_ => IsEventValue(_.Type));
        }
        return IsEventValue(type);
    }

    static bool IsEventValue(ITypeSymbol type)
    {
        if (type.ToDisplayString() == "Cratis.Chronicle.EventSequences.EventsWithConcurrencyScopes" ||
            type.ToDisplayString() == "Cratis.Chronicle.EventSequences.EventForEventSourceId" ||
            type.GetAttributes().Any(_ => _.AttributeClass?.ToDisplayString() == "Cratis.Chronicle.Events.EventTypeAttribute"))
        {
            return true;
        }
        if (type is INamedTypeSymbol { IsGenericType: true } named &&
            named.AllInterfaces.Concat([named]).Any(_ => _.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>"))
        {
            return named.TypeArguments.Any(_ => _.SpecialType == SpecialType.System_Object || IsEventValue(_));
        }
        return false;
    }

    static bool IsChronicleBacked(ITypeSymbol type, Compilation compilation)
    {
        if (type is not INamedTypeSymbol named || type.SpecialType != SpecialType.None)
        {
            return false;
        }
        if (named.GetAttributes().Any(_ => _.AttributeClass?.ContainingNamespace.ToDisplayString() == "Cratis.Chronicle.Projections.ModelBound" &&
            (_.AttributeClass.Name.StartsWith("From", StringComparison.Ordinal) || _.AttributeClass.Name.StartsWith("RemovedWith", StringComparison.Ordinal))))
        {
            return true;
        }
        return compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .Any(backing => backing.AllInterfaces.Any(_ => _.IsGenericType && _.TypeArguments.Length == 1 &&
                SymbolEqualityComparer.Default.Equals(_.TypeArguments[0], named) &&
                (_.OriginalDefinition.Name == "IProjectionFor" || _.OriginalDefinition.Name == "IReducerFor") &&
                (_.ContainingNamespace.ToDisplayString() == "Cratis.Chronicle.Projections" || _.ContainingNamespace.ToDisplayString() == "Cratis.Chronicle.Reducers")));
    }

    static bool IsImmediateAppend(InvocationExpressionSyntax syntax, IMethodSymbol method, SemanticModel model, CancellationToken cancellationToken)
    {
        if (!method.Name.StartsWith("Append", StringComparison.Ordinal) ||
            syntax.Expression is not MemberAccessExpressionSyntax member)
        {
            return false;
        }
        return model.GetTypeInfo(member.Expression, cancellationToken).Type is { } receiver && Implements(receiver, EventLog) &&
            (method.ContainingType.ToDisplayString() == "Cratis.Chronicle.EventSequences.IEventSequence" || Implements(method.ContainingType, EventLog));
    }

    static bool Implements(ITypeSymbol type, string fullName) => type.ToDisplayString() == fullName ||
        type.AllInterfaces.Any(_ => _.ToDisplayString() == fullName);
}
