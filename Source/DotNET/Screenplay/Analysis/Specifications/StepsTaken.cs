// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Answers whether a step written in a body is one the body always takes.
/// </summary>
/// <remarks>
/// A scenario says what happened, once, in order. A step written inside a branch happened in some runs of the
/// specification and not in others, and one written inside a loop happened as many times as the loop went round -
/// neither of which the source text says. Reading such a step as if it always happened states a world the
/// application was never specified against, which is worse than saying nothing: it is the one failure mode a reader
/// has no way of catching.
/// <para>
/// Which condition a branch stood on is routinely knowable to the compiler - a virtual property a derived
/// specification overrides with a constant is the usual shape - but knowing it means following values through
/// dispatch rather than reading what was written, which is a different discipline from the one everything else here
/// keeps to. So a conditional step is left unread and said so, and a scenario that has one is left out whole.
/// </para>
/// </remarks>
public static class StepsTaken
{
    /// <summary>
    /// Determines whether a body always takes a step, exactly once.
    /// </summary>
    /// <param name="call">The call the step is written as.</param>
    /// <param name="body">The body the step is written in.</param>
    /// <returns>True when nothing between the two makes the step conditional or repeated.</returns>
    public static bool Always(SyntaxNode call, SyntaxNode body)
    {
        for (var current = call.Parent; current is not null && current != body; current = current.Parent)
        {
            if (IsConditional(current))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether every normal completion of a body passes through a call, exactly once.
    /// </summary>
    /// <param name="call">The call to check.</param>
    /// <param name="body">The body the call is written in.</param>
    /// <returns>True when nothing around or before the call can skip or swallow it.</returns>
    /// <remarks>
    /// <see cref="Always"/> reads only what encloses the call. A guard written before it - <c>if (...) return;</c> -
    /// lets the body finish without reaching it just as much, and a <see langword="try"/> with a <see langword="catch"/> lets the body
    /// finish after the call failed. Either way the body can pass without the call having held.
    /// </remarks>
    public static bool AlwaysCompletesThrough(SyntaxNode call, SyntaxNode body)
    {
        if (!Always(call, body))
        {
            return false;
        }

        for (var node = call; node is not null && node != body; node = node.Parent)
        {
            if (node.Parent is TryStatementSyntax { Catches.Count: > 0 })
            {
                return false;
            }

            if (node.Parent is BlockSyntax block && block.Statements.TakeWhile(statement => statement != node)
                .SelectMany(statement => statement.DescendantNodesAndSelf(descendIntoChildren: child =>
                    child is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax)))
                .Any(statement => statement is ReturnStatementSyntax or ThrowStatementSyntax or ThrowExpressionSyntax or GotoStatementSyntax or YieldStatementSyntax))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether a construct makes what is written inside it conditional, repeated or deferred.
    /// </summary>
    /// <param name="node">The construct to check.</param>
    /// <returns>True when it does.</returns>
    static bool IsConditional(SyntaxNode node) => node switch
    {
        IfStatementSyntax or SwitchStatementSyntax or SwitchExpressionSyntax => true,
        ForStatementSyntax or ForEachStatementSyntax or WhileStatementSyntax or DoStatementSyntax => true,
        ConditionalExpressionSyntax or ConditionalAccessExpressionSyntax => true,
        CatchClauseSyntax or FinallyClauseSyntax => true,
        AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax => true,
        BinaryExpressionSyntax binary => binary.IsKind(SyntaxKind.LogicalAndExpression) ||
            binary.IsKind(SyntaxKind.LogicalOrExpression) ||
            binary.IsKind(SyntaxKind.CoalesceExpression),
        _ => false
    };
}
