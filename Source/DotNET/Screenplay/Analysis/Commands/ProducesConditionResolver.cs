// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Resolves the condition guarding an event construction, from the branches it sits inside.
/// </summary>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// A handler that decides between two events is the whole reason conditions exist in a document, so the decision is
/// read from the branch structure rather than left out. A branch that cannot be expressed produces the event
/// unconditionally and says so, which is wrong in a way the reader can see rather than wrong silently.
/// <para>
/// A body an aggregate root declares is read exactly like the handler's own, with the call site standing in for the
/// names the aggregate root gave the input. What it decides on its own state is the one thing that stays out,
/// because the language has nowhere to put it.
/// </para>
/// </remarks>
public class ProducesConditionResolver(ScreenplayDiagnostics diagnostics)
{
    readonly UnmappableConditions _unmappable = new(diagnostics);
    readonly PrecedingGuards _guards = new(diagnostics);
    readonly ConditionReader _conditions = new(diagnostics);

    /// <summary>
    /// Resolves the condition guarding an event construction, by walking out to the body it lives in.
    /// </summary>
    /// <param name="creation">The construction to resolve for.</param>
    /// <param name="body">The body the construction lives in.</param>
    /// <param name="scope">The <see cref="ProducesScope"/> the body was read in.</param>
    /// <param name="eventType">The type of the event being constructed.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <returns>The condition, or <see langword="null"/> when the production is unconditional.</returns>
    public ConditionModel? Resolve(
        SyntaxNode creation,
        SyntaxNode body,
        ProducesScope scope,
        ITypeSymbol eventType,
        string location)
    {
        ConditionModel? condition = null;

        for (var node = creation; node is not null && node != body; node = node.Parent)
        {
            var step = StepFor(node, scope, eventType, location);
            if (step is not null)
            {
                condition = condition is null ? step : new LogicalCondition(condition, false, step);
            }
        }

        return condition;
    }

    /// <summary>
    /// Resolves the condition one enclosing decision contributes.
    /// </summary>
    /// <param name="node">The node whose parent is the decision.</param>
    /// <param name="scope">The <see cref="ProducesScope"/> the node was read in.</param>
    /// <param name="eventType">The type of the event being constructed.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <returns>The condition, or <see langword="null"/> when the decision contributes none.</returns>
    ConditionModel? StepFor(SyntaxNode node, ProducesScope scope, ITypeSymbol eventType, string location)
    {
        switch (node.Parent)
        {
            case IfStatementSyntax statement:
                return Decision(
                    statement.Condition,
                    ReferenceEquals(node, statement.Statement),
                    ReferenceEquals(node, statement.Else),
                    scope,
                    eventType,
                    location);

            case ConditionalExpressionSyntax conditional:
                return Decision(
                    conditional.Condition,
                    ReferenceEquals(node, conditional.WhenTrue),
                    ReferenceEquals(node, conditional.WhenFalse),
                    scope,
                    eventType,
                    location);

            case BlockSyntax block:
                return _guards.In(block, node, scope, eventType, location);

            case SwitchSectionSyntax or SwitchExpressionArmSyntax:
                diagnostics.Warning(
                    ScreenplayDiagnosticCodes.UnmappableCommandProduction,
                    $"'{eventType.Name}' is produced from a switch, which has no counterpart in a produces condition, so it is stated unconditionally",
                    location);

                return null;

            default:
                return null;
        }
    }

    /// <summary>
    /// Resolves which side of a decision a production sits on.
    /// </summary>
    /// <param name="guard">The expression the decision is made on.</param>
    /// <param name="isPositive">Whether the production sits on the branch taken when the guard holds.</param>
    /// <param name="isNegative">Whether the production sits on the branch taken when it does not.</param>
    /// <param name="scope">The <see cref="ProducesScope"/> the decision was read in.</param>
    /// <param name="eventType">The type of the event being constructed.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <returns>The condition for the branch, or <see langword="null"/>.</returns>
    ConditionModel? Decision(
        ExpressionSyntax guard,
        bool isPositive,
        bool isNegative,
        ProducesScope scope,
        ITypeSymbol eventType,
        string location)
    {
        if (!isPositive && !isNegative)
        {
            return null;
        }

        var condition = _conditions.Read(guard, scope.SemanticModel, scope.Command, location, scope.Bindings);
        var resolved = isPositive ? condition : ConditionReader.Invert(condition);
        if (resolved is null)
        {
            _unmappable.Report(guard, scope, eventType, location);
        }

        return resolved;
    }
}
