// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Reads the events a command produces, from the body of its handler.
/// </summary>
/// <param name="compilation">The compilation being analyzed.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// Every event the handler constructs is a production, wherever in the body it happens - returned directly, wrapped
/// in a result, appended to a log or handed to a collection. Reading the construction is what gives the mappings
/// their values, rather than pairing names up and hoping.
/// </remarks>
public class ProducesReader(Compilation compilation, ScreenplayDiagnostics diagnostics)
{
    readonly ProducesMappingReader _mappings = new(diagnostics);
    readonly ProducesConditionResolver _conditions = new(diagnostics);

    /// <summary>
    /// Reads everything the handlers of a command produce.
    /// </summary>
    /// <param name="command">The type declaring the command.</param>
    /// <param name="handlers">The handler methods to read.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <returns>The productions, in the order the source declares them.</returns>
    public IEnumerable<ProducesModel> Read(INamedTypeSymbol command, IReadOnlyList<IMethodSymbol> handlers, string location)
    {
        var produces = new List<ProducesModel>();

        foreach (var handler in handlers)
        {
            ReportEventSourceIdResult(handler, location);

            foreach (var body in HandlerBodies.Of(handler))
            {
                ReadBody(command, body, location, produces);
            }
        }

        return produces.Count > 0 ? Deduplicate(produces) : FromSignatures(handlers, location);
    }

    /// <summary>
    /// Removes productions the source declared more than once, keeping the first.
    /// </summary>
    /// <param name="produces">The productions to reduce.</param>
    /// <returns>The distinct productions.</returns>
    static List<ProducesModel> Deduplicate(IEnumerable<ProducesModel> produces)
    {
        var kept = new List<ProducesModel>();

        foreach (var production in produces)
        {
            if (!kept.Exists(existing => IsSame(existing, production)))
            {
                kept.Add(production);
            }
        }

        return kept;
    }

    /// <summary>
    /// Determines whether two productions say the same thing.
    /// </summary>
    /// <param name="left">The first production.</param>
    /// <param name="right">The second production.</param>
    /// <returns>True when they are the same.</returns>
    static bool IsSame(ProducesModel left, ProducesModel right) =>
        string.Equals(left.EventName, right.EventName, StringComparison.Ordinal) &&
        Equals(left.When, right.When) &&
        left.Mappings.SequenceEqual(right.Mappings);

    /// <summary>
    /// Reads every event constructed within one handler body.
    /// </summary>
    /// <param name="command">The type declaring the command.</param>
    /// <param name="body">The body to read.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <param name="produces">The productions collected so far.</param>
    void ReadBody(INamedTypeSymbol command, SyntaxNode body, string location, List<ProducesModel> produces)
    {
        var semanticModel = compilation.GetSemanticModel(body.SyntaxTree);

        foreach (var creation in body.DescendantNodesAndSelf().OfType<BaseObjectCreationExpressionSyntax>())
        {
            if (semanticModel.GetTypeInfo(creation).Type is not INamedTypeSymbol type || !EventReader.IsEvent(type))
            {
                continue;
            }

            produces.Add(new(
                type.Name,
                _conditions.Resolve(creation, body, semanticModel, command, type, location),
                _mappings.Read(creation, semanticModel, command, type, location)));
        }
    }

    /// <summary>
    /// Falls back to what the signatures of the handlers promise when no body could be read.
    /// </summary>
    /// <param name="handlers">The handler methods to read.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <returns>The productions, without mappings.</returns>
    IEnumerable<ProducesModel> FromSignatures(IReadOnlyList<IMethodSymbol> handlers, string location)
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

    /// <summary>
    /// Reports a handler yielding the identifier of the event source it appends to.
    /// </summary>
    /// <param name="handler">The handler to check.</param>
    /// <param name="location">Where the command lives.</param>
    void ReportEventSourceIdResult(IMethodSymbol handler, string location)
    {
        if (HandlerBodies.YieldsEventSourceId(handler.ReturnType))
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult,
                "The handler yields the identifier of the event source alongside the event, which Screenplay has no counterpart for",
                location);
        }
    }
}
