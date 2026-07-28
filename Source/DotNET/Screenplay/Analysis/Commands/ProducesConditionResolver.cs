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
/// </remarks>
public class ProducesConditionResolver(ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Resolves the condition guarding an event construction, by walking out to the body it lives in.
    /// </summary>
    /// <param name="creation">The construction to resolve for.</param>
    /// <param name="body">The body the construction lives in.</param>
    /// <param name="semanticModel">The semantic model of the tree the construction lives in.</param>
    /// <param name="owner">The type whose properties count as the command's own input.</param>
    /// <param name="eventType">The type of the event being constructed.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <returns>The condition, or <see langword="null"/> when the production is unconditional.</returns>
    public ConditionModel? Resolve(
        SyntaxNode creation,
        SyntaxNode body,
        SemanticModel semanticModel,
        ITypeSymbol owner,
        ITypeSymbol eventType,
        string location)
    {
        ConditionModel? condition = null;

        for (var node = creation; node is not null && node != body; node = node.Parent)
        {
            var step = StepFor(node, semanticModel, owner, eventType, location);
            if (step is not null)
            {
                condition = condition is null ? step : new LogicalCondition(condition, false, step);
            }
        }

        return condition;
    }

    /// <summary>
    /// Resolves what the guard clauses a statement sits after say about when it is reached.
    /// </summary>
    /// <param name="block">The block the statement sits in.</param>
    /// <param name="node">The statement.</param>
    /// <param name="semanticModel">The semantic model of the tree the block lives in.</param>
    /// <param name="owner">The type whose properties count as the command's own input.</param>
    /// <returns>The condition, or <see langword="null"/> when nothing guards the statement.</returns>
    /// <remarks>
    /// A handler that returns one event from inside a branch and another after it has decided between the two just
    /// as much as one that writes the second branch out. Without reading the guard, the second event would be
    /// stated as always produced, which describes a different application rather than an incomplete one.
    /// </remarks>
    static ConditionModel? GuardsBefore(BlockSyntax block, SyntaxNode node, SemanticModel semanticModel, ITypeSymbol owner)
    {
        ConditionModel? condition = null;

        foreach (var statement in block.Statements.TakeWhile(_ => !ReferenceEquals(_, node)))
        {
            if (statement is not IfStatementSyntax { Else: null } guard || !AlwaysExits(guard.Statement))
            {
                continue;
            }

            var inverted = ConditionReader.Invert(ConditionReader.Read(guard.Condition, semanticModel, owner));
            if (inverted is not null)
            {
                condition = condition is null ? inverted : new LogicalCondition(condition, false, inverted);
            }
        }

        return condition;
    }

    /// <summary>
    /// Determines whether a branch always leaves the handler, making everything after it the other outcome.
    /// </summary>
    /// <param name="statement">The branch to check.</param>
    /// <returns>True when the branch always exits.</returns>
    static bool AlwaysExits(StatementSyntax statement) => statement switch
    {
        ReturnStatementSyntax or ThrowStatementSyntax => true,
        BlockSyntax block => block.Statements.Count > 0 && AlwaysExits(block.Statements[^1]),
        _ => false
    };

    /// <summary>
    /// Resolves the condition one enclosing decision contributes.
    /// </summary>
    /// <param name="node">The node whose parent is the decision.</param>
    /// <param name="semanticModel">The semantic model of the tree the node lives in.</param>
    /// <param name="owner">The type whose properties count as the command's own input.</param>
    /// <param name="eventType">The type of the event being constructed.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <returns>The condition, or <see langword="null"/> when the decision contributes none.</returns>
    ConditionModel? StepFor(
        SyntaxNode node,
        SemanticModel semanticModel,
        ITypeSymbol owner,
        ITypeSymbol eventType,
        string location)
    {
        switch (node.Parent)
        {
            case IfStatementSyntax statement:
                return Branch(
                    ConditionReader.Read(statement.Condition, semanticModel, owner),
                    ReferenceEquals(node, statement.Statement),
                    ReferenceEquals(node, statement.Else),
                    eventType,
                    location);

            case ConditionalExpressionSyntax conditional:
                return Branch(
                    ConditionReader.Read(conditional.Condition, semanticModel, owner),
                    ReferenceEquals(node, conditional.WhenTrue),
                    ReferenceEquals(node, conditional.WhenFalse),
                    eventType,
                    location);

            case BlockSyntax block:
                return GuardsBefore(block, node, semanticModel, owner);

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
    /// <param name="condition">The condition of the decision.</param>
    /// <param name="isPositive">Whether the production sits on the branch taken when the condition holds.</param>
    /// <param name="isNegative">Whether the production sits on the branch taken when it does not.</param>
    /// <param name="eventType">The type of the event being constructed.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <returns>The condition for the branch, or <see langword="null"/>.</returns>
    ConditionModel? Branch(ConditionModel? condition, bool isPositive, bool isNegative, ITypeSymbol eventType, string location)
    {
        if (!isPositive && !isNegative)
        {
            return null;
        }

        var resolved = isPositive ? condition : ConditionReader.Invert(condition);
        if (resolved is null)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableCommandProduction,
                $"The branch producing '{eventType.Name}' is guarded by code that has no counterpart in a produces condition, so it is stated unconditionally",
                location);
        }

        return resolved;
    }
}
