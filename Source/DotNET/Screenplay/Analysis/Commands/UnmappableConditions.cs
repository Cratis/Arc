// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Aggregates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Reports the guards a <c>produces when</c> condition cannot carry.
/// </summary>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// Two very different things end up in the same place - a guard that could not be read, and a guard that was read
/// perfectly well and has nowhere to go. An aggregate root deciding on its own state is the second, and saying which
/// one happened is the difference between a reader looking for a bug in the generator and a reader understanding
/// that the language stops there.
/// </remarks>
public class UnmappableConditions(ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Reports a guard that could not be carried into a condition, whatever the reason.
    /// </summary>
    /// <param name="guard">The guard expression.</param>
    /// <param name="scope">The <see cref="ProducesScope"/> the guard was read in.</param>
    /// <param name="eventType">The type of the event being produced.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    public void Report(ExpressionSyntax guard, ProducesScope scope, ITypeSymbol eventType, string location)
    {
        if (ReportAggregateState(guard, scope, eventType, location))
        {
            return;
        }

        diagnostics.Warning(
            ScreenplayDiagnosticCodes.UnmappableCommandProduction,
            $"The branch producing '{eventType.Name}' is guarded by code that has no counterpart in a produces condition, so it is stated unconditionally",
            location);
    }

    /// <summary>
    /// Reports a guard reading the state of an aggregate root, and nothing else.
    /// </summary>
    /// <param name="guard">The guard expression.</param>
    /// <param name="scope">The <see cref="ProducesScope"/> the guard was read in.</param>
    /// <param name="eventType">The type of the event being produced.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <returns>True when the guard reads aggregate root state and was reported.</returns>
    public bool ReportAggregateState(ExpressionSyntax guard, ProducesScope scope, ITypeSymbol eventType, string location)
    {
        if (!AggregateState.IsReadBy(guard, scope.SemanticModel, scope.AggregateRoot))
        {
            return false;
        }

        diagnostics.Warning(
            ScreenplayDiagnosticCodes.UnmappableAggregateStateCondition,
            $"The branch producing '{eventType.Name}' is guarded by the state '{scope.AggregateRoot!.Name}' holds, and a produces condition can only compare the input of the command, so it is stated unconditionally",
            location);

        return true;
    }
}
