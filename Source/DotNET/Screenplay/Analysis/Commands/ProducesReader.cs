// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Aggregates;
using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Reads the events a command produces, from the body of its handler.
/// </summary>
/// <param name="models">The <see cref="SemanticModels"/> every body is read through.</param>
/// <param name="aggregates">The <see cref="AggregateRootCatalog"/> recording which aggregate roots a command reaches.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// Every event the handler constructs is a production, wherever in the body it happens - returned directly, wrapped
/// in a result, appended to a log or handed to a collection. Reading the construction is what gives the mappings
/// their values, rather than pairing names up and hoping.
/// <para>
/// A handler that governs its change through an aggregate root constructs nothing itself, so the behaviors it calls
/// are read as well. Both ways of writing an Arc command then describe the same thing in the document.
/// </para>
/// <para>
/// That behavior is the one body here that need not belong to the project the command does - an aggregate root in a
/// domain project called from the project above it is the ordinary layered arrangement - so which model reads it is
/// asked rather than assumed.
/// </para>
/// </remarks>
public class ProducesReader(SemanticModels models, AggregateRootCatalog aggregates, ScreenplayDiagnostics diagnostics)
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
                if (models.For(body.SyntaxTree) is not { } semanticModel)
                {
                    continue;
                }

                ReadBody(command, body, semanticModel, location, produces, null);

                foreach (var behavior in AggregateRootBehaviors.ReachedFrom(body, semanticModel))
                {
                    aggregates.Reached(behavior.AggregateRoot);
                    ReadBehavior(command, behavior, location, produces);
                }
            }
        }

        return produces.Count > 0 ? Deduplicate(produces) : ProducedBySignature.Of(handlers, location, diagnostics);
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
    /// <param name="semanticModel">The model the body is read through.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <param name="produces">The productions collected so far.</param>
    /// <param name="behavior">The behavior the body belongs to, when the handler reached it through an aggregate root.</param>
    void ReadBody(
        INamedTypeSymbol command,
        SyntaxNode body,
        SemanticModel semanticModel,
        string location,
        List<ProducesModel> produces,
        AggregateRootInvocation? behavior)
    {
        var scope = new ProducesScope(semanticModel, command, behavior?.Bindings, behavior?.AggregateRoot);

        foreach (var creation in body.DescendantNodesAndSelf().OfType<BaseObjectCreationExpressionSyntax>())
        {
            if (semanticModel.GetTypeInfo(creation).Type is not INamedTypeSymbol type || !EventReader.IsEvent(type))
            {
                continue;
            }

            produces.Add(new(
                type.Name,
                _conditions.Resolve(creation, body, scope, type, location),
                _mappings.Read(creation, semanticModel, command, type, location, scope.Bindings)));
        }
    }

    /// <summary>
    /// Reads what a behavior of an aggregate root the handler reached produces.
    /// </summary>
    /// <param name="command">The type declaring the command.</param>
    /// <param name="behavior">The behavior the handler reached.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <param name="produces">The productions collected so far.</param>
    /// <remarks>
    /// The behavior is written wherever the aggregate root is, and a project that was not handed over is one whose
    /// source cannot be read at all. Saying so is the difference between a command stated as producing nothing and a
    /// reader who knows a project is missing from what the document was generated from.
    /// </remarks>
    void ReadBehavior(
        INamedTypeSymbol command,
        AggregateRootInvocation behavior,
        string location,
        List<ProducesModel> produces)
    {
        if (models.For(behavior.Body.SyntaxTree) is { } semanticModel)
        {
            ReadBody(command, behavior.Body, semanticModel, location, produces, behavior);

            return;
        }

        diagnostics.Warning(
            ScreenplayDiagnosticCodes.UnmappableCommandProduction,
            $"The command hands its work to '{behavior.AggregateRoot.Name}', which is written in a project the document was not generated from, so what it applies is not stated",
            location);
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
