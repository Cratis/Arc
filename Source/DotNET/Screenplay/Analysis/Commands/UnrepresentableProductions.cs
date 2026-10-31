// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Withholds unsafe productions and prevents signature recovery from restoring them.
/// </summary>
/// <param name="diagnostics">Where unrepresentable destinations are reported.</param>
internal class UnrepresentableProductions(ScreenplayDiagnostics diagnostics)
{
    bool _withheld;

    /// <summary>
    /// Withholds unsafe generated productions or a proven unsafe ordinary returned-tuple wrapper's handler.
    /// </summary>
    /// <param name="handler">The handler whose productions were read.</param>
    /// <param name="produces">The productions collected so far.</param>
    /// <param name="range">The handler's first production and returned-tuple wrapper proof.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    internal void Apply(IMethodSymbol handler, List<ProducesModel> produces, (int First, bool UnsafeReturnedTupleWrapper) range, string location)
    {
        var generated = handler.Parameters.Any(parameter => parameter.HasAttribute(WellKnownTypeNames.GeneratedValueAttribute));
        if (!(generated ? produces.Skip(range.First).Any(IsUnrepresentable) : range.UnsafeReturnedTupleWrapper))
        {
            return;
        }

        produces.RemoveRange(range.First, produces.Count - range.First);
        diagnostics.Information(
            ScreenplayDiagnosticCodes.UnrepresentableProductionDestination,
            "A production of the command is explicitly routed or does not demonstrably use command context, so no identifier or for destination was stated and no production of the command is stated",
            location);
        _withheld = true;
    }

    /// <summary>
    /// Reads signature productions only when no body productions were withheld.
    /// </summary>
    /// <param name="handlers">The handler methods to read.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <returns>The fallback productions, or none when withholding prevents recovery.</returns>
    internal IEnumerable<ProducesModel> ReadSignature(IReadOnlyList<IMethodSymbol> handlers, string location) =>
        _withheld ? [] : ProducedBySignature.Of(handlers, location, diagnostics);

    static bool IsUnrepresentable(ProducesModel production) => !production.UsesCommandContext && production.Destination is null &&
        production.UnavailableDestination is null && production.UnavailableCondition is null && production.DestinationDependencies.Count > 0;
}
