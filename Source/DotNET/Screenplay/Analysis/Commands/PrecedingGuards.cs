// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Reads what the guard clauses a statement sits after say about when it is reached.
/// </summary>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// A body that leaves early on one outcome and produces the other afterwards has decided between the two just as
/// much as one that writes both branches out. Without reading the guard, the second event would be stated as always
/// produced, which describes a different application rather than an incomplete one.
/// <para>
/// This is where the state of an aggregate root most often decides something - a behavior refusing to act on what it
/// has already seen - so a guard reading that state is reported rather than passed over in silence.
/// </para>
/// </remarks>
public class PrecedingGuards(ScreenplayDiagnostics diagnostics)
{
    readonly UnmappableConditions _unmappable = new(diagnostics);
    readonly ConditionReader _conditions = new(diagnostics);

    /// <summary>
    /// Reads the guard clauses standing between the start of a block and a statement in it.
    /// </summary>
    /// <param name="block">The block the statement sits in.</param>
    /// <param name="node">The statement.</param>
    /// <param name="scope">The <see cref="ProducesScope"/> the block was read in.</param>
    /// <param name="eventType">The type of the event being produced.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <returns>The condition, or <see langword="null"/> when nothing guards the statement.</returns>
    public ConditionModel? In(
        BlockSyntax block,
        SyntaxNode node,
        ProducesScope scope,
        ITypeSymbol eventType,
        string location)
    {
        ConditionModel? condition = null;

        foreach (var statement in block.Statements.TakeWhile(_ => !ReferenceEquals(_, node)))
        {
            if (statement is not IfStatementSyntax { Else: null } guard || !AlwaysExits(guard.Statement))
            {
                continue;
            }

            var read = _conditions.Read(guard.Condition, scope.SemanticModel, scope.Command, location, scope.Bindings);
            var inverted = ConditionReader.Invert(read);
            if (inverted is null)
            {
                _unmappable.ReportAggregateState(guard.Condition, scope, eventType, location);
                continue;
            }

            condition = condition is null ? inverted : new LogicalCondition(condition, false, inverted);
        }

        return condition;
    }

    /// <summary>
    /// Determines whether a branch always leaves the body, making everything after it the other outcome.
    /// </summary>
    /// <param name="statement">The branch to check.</param>
    /// <returns>True when the branch always exits.</returns>
    static bool AlwaysExits(StatementSyntax statement) => statement switch
    {
        ReturnStatementSyntax or ThrowStatementSyntax => true,
        BlockSyntax block => block.Statements.Count > 0 && AlwaysExits(block.Statements[^1]),
        _ => false
    };
}
