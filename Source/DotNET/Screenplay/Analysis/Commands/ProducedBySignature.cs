// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// States what the signature of a handler promises, for a command whose body could not be read.
/// </summary>
/// <remarks>
/// Reading the body is what gives a production its mappings, so this is strictly the lesser answer - it says which
/// events a command produces and nothing about what it puts in them. It is still much better than silence: a body
/// that cannot be read is a partial documentation of the command rather than an argument for describing none of it,
/// and what was lost is reported so the difference is visible.
/// </remarks>
public static class ProducedBySignature
{
    /// <summary>
    /// Reads what the signatures of a command's handlers promise.
    /// </summary>
    /// <param name="handlers">The handler methods to read.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <returns>The productions, without mappings.</returns>
    public static IEnumerable<ProducesModel> Of(
        IReadOnlyList<IMethodSymbol> handlers,
        string location,
        ScreenplayDiagnostics diagnostics)
    {
        var events = handlers
            .SelectMany(_ => HandlerBodies.EventTypesIn(_.ReturnType))
            .Select(_ => _.Name)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        foreach (var name in events)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableCommandProduction,
                $"'{name}' is named by the handler's signature but never constructed in a body that could be read, so it is stated without mappings",
                location);
        }

        return [.. events.Select(_ => new ProducesModel(_, null, []))];
    }
}
