// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Binds a single unconditional awaited execution to its held slice and optional outcome.
/// </summary>
/// <param name="expression">The complete action expression.</param>
/// <param name="run">The exact shipped Run invocation.</param>
/// <param name="model">The semantic model of the action.</param>
internal sealed class SliceActionProof(ExpressionSyntax expression, InvocationExpressionSyntax run, SemanticModel model)
{
    /// <summary>
    /// The metadata name of the shipped slice type.
    /// </summary>
    internal const string SliceType = "Cratis.Arc.Chronicle.Testing.Slices.Slice`1";

    /// <summary>
    /// The metadata name of the shipped slice outcome type.
    /// </summary>
    internal const string OutcomeType = "Cratis.Arc.Chronicle.Testing.Slices.SliceOutcome`1";

    /// <summary>
    /// Gets the direct held slice receiver of Run.
    /// </summary>
    internal ExpressionSyntax Receiver => ((MemberAccessExpressionSyntax)run.Expression).Expression;

    /// <summary>
    /// Gets the semantic model of the action.
    /// </summary>
    internal SemanticModel Model => model;

    /// <summary>
    /// Gets the exact awaited Run invocation.
    /// </summary>
    internal InvocationExpressionSyntax Run => run;

    /// <summary>
    /// Gets the held slice field.
    /// </summary>
    internal IFieldSymbol Slice => (IFieldSymbol)model.GetSymbolInfo(Receiver).Symbol!;

    /// <summary>
    /// Gets the command type executed by the slice.
    /// </summary>
    internal ITypeSymbol Command => ((IMethodSymbol)model.GetSymbolInfo(run).Symbol!).ContainingType.TypeArguments[0];

    /// <summary>
    /// Gets the symbol receiving the outcome, or null when no outcome is assigned.
    /// </summary>
    internal ISymbol? Outcome => expression is AssignmentExpressionSyntax assignment ? model.GetSymbolInfo(assignment.Left).Symbol : null;

    /// <summary>
    /// Reads exactly one direct stable field receiver and one unconditional awaited Run.
    /// </summary>
    /// <param name="steps">The complete inherited step chain.</param>
    /// <param name="models">The semantic models of the source.</param>
    /// <param name="held">The held-value stability reader.</param>
    /// <returns>The action binding, or null when its execution cannot be proved.</returns>
    internal static SliceActionProof? TryRead(INamedTypeSymbol steps, SemanticModels models, HeldValues held)
    {
        var actions = SpecificationMembers.MethodsIn(steps, SpecificationMembers.BecauseMethod).SelectMany(HandlerBodies.Of).ToList();
        if (actions is not [var body] || ExpressionOf(body) is not { } expression ||
            AwaitedRun(expression) is not { Expression: MemberAccessExpressionSyntax receiver } run ||
            models.For(run.SyntaxTree) is not { } model ||
            model.GetSymbolInfo(run).Symbol is not IMethodSymbol method || !IsRun(method) ||
            !IsDirect(receiver.Expression) || model.GetSymbolInfo(receiver.Expression).Symbol is not IFieldSymbol slice ||
            !IsHeldBy(steps, slice) || !held.IsStable(slice, model.Compilation))
        {
            return null;
        }

        return new(expression, run, model);
    }

    /// <summary>
    /// Determines whether an expression is a direct name or explicit this receiver.
    /// </summary>
    /// <param name="expression">The reference expression to inspect.</param>
    /// <returns>True when the expression directly names a member.</returns>
    internal static bool IsDirect(ExpressionSyntax expression) => expression is IdentifierNameSyntax or MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax };

    /// <summary>
    /// Gets the single expression stated by a step or assertion body.
    /// </summary>
    /// <param name="body">The body to inspect.</param>
    /// <returns>The expression, or null when the body is not a single expression.</returns>
    internal static ExpressionSyntax? ExpressionOf(SyntaxNode body) => body switch
    {
        ExpressionSyntax expression => expression,
        BlockSyntax { Statements: [ExpressionStatementSyntax statement] } => statement.Expression,
        _ => null
    };

    /// <summary>
    /// Determines whether the optional outcome assignment targets a direct stable held field.
    /// </summary>
    /// <param name="steps">The inherited step chain holding the outcome.</param>
    /// <param name="held">The held-value stability reader.</param>
    /// <returns>True when no outcome is assigned or its held field is stable.</returns>
    internal bool HasStableOutcome(INamedTypeSymbol steps, HeldValues held) =>
        expression is not AssignmentExpressionSyntax assignment ||
        (Outcome is IFieldSymbol outcome && IsDirect(assignment.Left) && IsHeldBy(steps, outcome) && held.IsStable(outcome, model.Compilation));

    /// <summary>
    /// Determines whether a symbol names this action's slice or outcome.
    /// </summary>
    /// <param name="symbol">The referenced symbol to inspect.</param>
    /// <returns>True when the symbol is one of the action's held values.</returns>
    internal bool RefersToHeldValue(ISymbol symbol) =>
        SymbolEqualityComparer.Default.Equals(symbol, Slice) ||
        (Outcome is { } outcome && SymbolEqualityComparer.Default.Equals(symbol, outcome));

    /// <summary>
    /// Gets the admitted Run receiver and optional outcome write sites.
    /// </summary>
    /// <returns>The exact syntax sites admitted by the action.</returns>
    internal IEnumerable<SyntaxNode> ReferenceSites() => expression is AssignmentExpressionSyntax assignment
        ? [Receiver, assignment.Left]
        : [Receiver];

    static bool IsHeldBy(INamedTypeSymbol steps, ISymbol symbol) =>
        SpecificationMembers.ChainOf(steps).SelectMany(item => item.GetMembers())
            .Any(member => SymbolEqualityComparer.Default.Equals(member, symbol));

    static InvocationExpressionSyntax? AwaitedRun(ExpressionSyntax expression) => expression switch
    {
        AwaitExpressionSyntax { Expression: InvocationExpressionSyntax invocation } => invocation,
        AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression,
            Right: AwaitExpressionSyntax { Expression: InvocationExpressionSyntax invocation } } => invocation,
        _ => null
    };

    static bool IsRun(IMethodSymbol method) =>
        string.Equals(method.Name, "Run", StringComparison.Ordinal) &&
        method is { IsStatic: false, Arity: 0, Parameters.Length: 0 } && method.ContainingType.Is(SliceType) &&
        method.ReturnType is INamedTypeSymbol { TypeArguments: [INamedTypeSymbol outcome] } task &&
        task.Is("System.Threading.Tasks.Task`1") && outcome.Is(OutcomeType) &&
        SymbolEqualityComparer.Default.Equals(outcome.TypeArguments[0], method.ContainingType.TypeArguments[0]);
}
