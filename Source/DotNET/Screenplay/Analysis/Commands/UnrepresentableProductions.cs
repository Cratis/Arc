// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Withholds unsafe generated productions and prevents signature recovery from restoring them.
/// </summary>
/// <param name="diagnostics">Where unrepresentable destinations are reported.</param>
internal class UnrepresentableProductions(ScreenplayDiagnostics diagnostics)
{
    bool _withheld;

    /// <summary>
    /// Withholds every production of a handler with generated inputs when any has no representable destination.
    /// </summary>
    /// <param name="handler">The handler whose productions were read.</param>
    /// <param name="produces">The productions collected so far.</param>
    /// <param name="first">Where the handler's productions start.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    internal void Apply(IMethodSymbol handler, List<ProducesModel> produces, int first, string location)
    {
        if (!handler.Parameters.Any(parameter => parameter.HasAttribute(WellKnownTypeNames.GeneratedValueAttribute)) ||
            !produces.Skip(first).Any(IsUnrepresentable))
        {
            return;
        }

        produces.RemoveRange(first, produces.Count - first);
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
