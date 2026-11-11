// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Aggregates;
using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Analysis.Types;
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
/// <param name="authoringOnlyConstructs">Whether authoring-only constructs are enabled.</param>
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
public class ProducesReader(SemanticModels models, AggregateRootCatalog aggregates, ScreenplayDiagnostics diagnostics, bool authoringOnlyConstructs = false)
{
    readonly ProducesMappingReader _mappings = new(diagnostics);
    readonly ProducesConditionResolver _conditions = new(diagnostics);

    /// <summary>
    /// Gets the shared registry used to preserve explicit destination and stream key types.
    /// </summary>
    internal TypeRegistry? Types { get; init; }

    /// <summary>
    /// Gets analyzed definitions used to resolve stored legacy route names without guessing aliases.
    /// </summary>
    internal IReadOnlyList<INamedTypeSymbol> Definitions { get; init; } = [];

    /// <summary>
    /// Reads everything the handlers of a command produce.
    /// </summary>
    /// <param name="command">The type declaring the command.</param>
    /// <param name="handlers">The handler methods to read.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <param name="sources">Proven authoring-only mapping sources, when enabled.</param>
    /// <param name="generatedIdentity">Whether a returned generated identity proves the tuple destination.</param>
    /// <returns>The productions, in the order the source declares them.</returns>
    public IEnumerable<ProducesModel> Read(INamedTypeSymbol command, IReadOnlyList<IMethodSymbol> handlers, string location, AuthoringSources? sources = null, bool generatedIdentity = false)
    {
        var request = new ProducesReadRequest(command, location, [], sources, generatedIdentity);
        var withholding = new UnrepresentableProductions(diagnostics);
        var identifier = new CommandIdentifierReader(models, new ScreenplayDiagnostics()).Read(command, location);

        foreach (var handler in handlers)
        {
            ReadHandler(handler, request, identifier, withholding);
        }

        return request.Produces.Count > 0 ? Deduplicate(request.Produces) : withholding.ReadSignature(handlers, location);
    }

    /// <summary>Reads an action with only its classified request-body parameter admitted as command input.</summary>
    /// <param name="controller">The controller declaring the action.</param>
    /// <param name="handler">The action being read.</param>
    /// <param name="input">The request-body parameter already classified by the controller reader.</param>
    /// <param name="location">The action's source location.</param>
    /// <returns>The recovered productions.</returns>
    internal IEnumerable<ProducesModel> ReadController(INamedTypeSymbol controller, IMethodSymbol handler, IParameterSymbol? input, string location)
    {
        var reader = new ProducesReader(models, aggregates, diagnostics, authoringOnlyConstructs) { Types = Types, Definitions = Definitions };
        reader._mappings.ControllerInput = input;
        return reader.Read(controller, [handler], location);
    }

    /// <summary>
    /// Removes productions the source declared more than once, conservatively merging their eligibility flags.
    /// </summary>
    /// <param name="produces">The productions to reduce.</param>
    /// <returns>The distinct productions.</returns>
    static List<ProducesModel> Deduplicate(IEnumerable<ProducesModel> produces)
    {
        var kept = new List<ProducesModel>();

        foreach (var production in produces)
        {
            var index = kept.FindIndex(existing => IsSame(existing, production));
            if (index < 0)
            {
                kept.Add(production);
            }
            else
            {
                kept[index] = kept[index] with
                {
                    CanInline = kept[index].CanInline && production.CanInline,
                    UsesCommandContext = kept[index].UsesCommandContext && production.UsesCommandContext
                };
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
        left.EventTypeIdentity == right.EventTypeIdentity &&
        HaveSameConditionsAndDestinations(left, right) &&
        left.DestinationDependencies.SequenceEqual(right.DestinationDependencies) &&
        left.Mappings.SequenceEqual(right.Mappings);

    /// <summary>
    /// Determines whether two productions share their conditions and destinations.
    /// </summary>
    /// <param name="left">The first production.</param>
    /// <param name="right">The second production.</param>
    /// <returns>True when they share them.</returns>
    static bool HaveSameConditionsAndDestinations(ProducesModel left, ProducesModel right) =>
        Equals(left.When, right.When) &&
        Equals(left.Destination, right.Destination) &&
        Equals(left.Route, right.Route) &&
        Equals(left.UnavailableCondition, right.UnavailableCondition) &&
        Equals(left.UnavailableDestination, right.UnavailableDestination);

    /// <summary>
    /// Determines whether a handler's yielded event source identifier needs reporting.
    /// </summary>
    /// <param name="handler">The handler to check.</param>
    /// <param name="request">What the read is collecting into.</param>
    /// <param name="firstProduction">The index of the first production the handler contributed.</param>
    /// <returns>True when it should be reported.</returns>
    static bool ShouldReportEventSourceIdResult(IMethodSymbol handler, ProducesReadRequest request, int firstProduction) =>
        !request.GeneratedIdentity &&
        !EventResponseTuples.UsesCommandContext(handler.ReturnType) &&
        (request.Produces.Count == firstProduction || request.Produces.Skip(firstProduction).Any(production => !production.UsesCommandContext && production.Destination is null));

    /// <summary>
    /// Describes a body that is about to be read.
    /// </summary>
    /// <param name="request">What the read is collecting into.</param>
    /// <param name="syntax">The body.</param>
    /// <param name="semanticModel">The model the body is read through.</param>
    /// <param name="behavior">The behavior the body belongs to, when reached through an aggregate root.</param>
    /// <returns>The <see cref="ProducesBodyRead"/> describing the body.</returns>
    static ProducesBodyRead BodyOf(ProducesReadRequest request, SyntaxNode syntax, SemanticModel semanticModel, AggregateRootInvocation? behavior)
    {
        var scope = new ProducesScope(semanticModel, request.Command, behavior?.Bindings, behavior?.AggregateRoot) { ConsumedGuards = request.Sources?.ConsumedGuards };
        return new ProducesBodyRead(syntax, semanticModel, scope, behavior, false, null);
    }

    /// <summary>
    /// Gets the event type a construction creates, unless it is not an event or its effects must not be stated.
    /// </summary>
    /// <param name="body">The body the construction is in.</param>
    /// <param name="creation">The construction to inspect.</param>
    /// <returns>The event type, or null when the construction is not read.</returns>
    static INamedTypeSymbol? EventTypeOf(ProducesBodyRead body, BaseObjectCreationExpressionSyntax creation)
    {
        if (body.Model.GetTypeInfo(creation).Type is not INamedTypeSymbol type || !EventReader.IsEvent(type))
        {
            return null;
        }

        return body.Refusals?.Covers(creation) == true ? null : type;
    }

    /// <summary>
    /// Marks productions from an index onwards as not inlinable.
    /// </summary>
    /// <param name="produces">The productions collected so far.</param>
    /// <param name="first">The index of the first production to mark.</param>
    static void PreventInlining(List<ProducesModel> produces, int first)
    {
        for (var index = first; index < produces.Count; index++)
        {
            produces[index] = produces[index] with { CanInline = false };
        }
    }

    /// <summary>
    /// Reads everything one handler produces, from each of its bodies and the aggregate root behaviors they reach.
    /// </summary>
    /// <param name="handler">The handler to read.</param>
    /// <param name="request">What the read is collecting into.</param>
    /// <param name="identifier">The name of the command's identifier, when it has one.</param>
    /// <param name="withholding">The productions that cannot be represented.</param>
    void ReadHandler(IMethodSymbol handler, ProducesReadRequest request, string? identifier, UnrepresentableProductions withholding)
    {
        var firstProduction = request.Produces.Count;
        var (unsafeReturnedTupleWrapper, refusedReturnedTuple) = ReadHandlerBodies(handler, request, identifier);
        if (ShouldReportEventSourceIdResult(handler, request, firstProduction))
        {
            ReportEventSourceIdResult(request.Command, handler, request.Location);
        }

        withholding.Apply(handler, request.Produces, (firstProduction, unsafeReturnedTupleWrapper), request.Location);
        withholding.Refuse(refusedReturnedTuple, request.Produces.Count > firstProduction, request.Location);
    }

    /// <summary>
    /// Reads every body of a handler.
    /// </summary>
    /// <param name="handler">The handler to read.</param>
    /// <param name="request">What the read is collecting into.</param>
    /// <param name="identifier">The name of the command's identifier, when it has one.</param>
    /// <returns>Whether an unrepresentable returned tuple wrapper was met, and whether a returned tuple was refused.</returns>
    (bool UnsafeReturnedTupleWrapper, bool RefusedReturnedTuple) ReadHandlerBodies(IMethodSymbol handler, ProducesReadRequest request, string? identifier)
    {
        var unsafeReturnedTupleWrapper = false;
        var refusedReturnedTuple = false;
        foreach (var syntax in HandlerBodies.Of(handler))
        {
            if (models.For(syntax.SyntaxTree) is not { } semanticModel)
            {
                continue;
            }

            var usesCommandContext = request.GeneratedIdentity || !HandlerBodies.YieldsEventSourceId(handler.ReturnType) ||
                EventResponseTuples.UsesCommandContext(handler.ReturnType);
            var refusals = ReturnedTupleRefusals.For(handler, syntax, semanticModel, Types is not null);
            refusedReturnedTuple |= refusals.Any;
            var body = BodyOf(request, syntax, semanticModel, null) with { UsesCommandContext = usesCommandContext, Refusals = refusals };
            unsafeReturnedTupleWrapper |= ReadBody(request, body);
            ReadReachedBehaviors(request, body, identifier);
        }

        return (unsafeReturnedTupleWrapper, refusedReturnedTuple);
    }

    /// <summary>
    /// Reads the aggregate root behaviors a body reaches.
    /// </summary>
    /// <param name="request">What the read is collecting into.</param>
    /// <param name="body">The body that reaches them.</param>
    /// <param name="identifier">The name of the command's identifier, when it has one.</param>
    void ReadReachedBehaviors(ProducesReadRequest request, ProducesBodyRead body, string? identifier)
    {
        foreach (var behavior in AggregateRootBehaviors.ReachedFrom(body.Syntax, body.Model))
        {
            aggregates.Reached(behavior.AggregateRoot);
            var first = request.Produces.Count;
            var aggregateUsesCommandContext = body.UsesCommandContext &&
                ProductionDestinations.AggregateUsesCommandContext(behavior, body.Syntax, body.Model, request.Command, identifier);
            ReadBehavior(request, behavior, aggregateUsesCommandContext);
            if (!InlineProductionShape.IsUnconditional(body.Syntax))
            {
                PreventInlining(request.Produces, first);
            }
        }
    }

    /// <summary>
    /// Reads every event constructed within one handler body.
    /// </summary>
    /// <param name="request">What the read is collecting into.</param>
    /// <param name="body">The body to read.</param>
    /// <returns>Whether a supported returned tuple has an explicitly unrepresentable wrapper.</returns>
    bool ReadBody(ProducesReadRequest request, ProducesBodyRead body)
    {
        var unsafeReturnedTupleWrapper = false;

        foreach (var creation in body.Syntax.DescendantNodesAndSelf().OfType<BaseObjectCreationExpressionSyntax>())
        {
            if (EventTypeOf(body, creation) is not { } type)
            {
                continue;
            }

            var routed = ReadCreation(request, body, creation, type);
            request.Produces.Add(routed.Production);
            unsafeReturnedTupleWrapper |= routed.UnsafeReturnedTupleWrapper;
        }

        return unsafeReturnedTupleWrapper;
    }

    /// <summary>
    /// Reads the production one event construction states.
    /// </summary>
    /// <param name="request">What the read is collecting into.</param>
    /// <param name="body">The body the construction is in.</param>
    /// <param name="creation">The construction of the event.</param>
    /// <param name="type">The event type constructed.</param>
    /// <returns>The production, and whether it has an unrepresentable returned tuple wrapper.</returns>
    (ProducesModel Production, bool UnsafeReturnedTupleWrapper) ReadCreation(ProducesReadRequest request, ProducesBodyRead body, BaseObjectCreationExpressionSyntax creation, INamedTypeSymbol type)
    {
        var sources = request.Sources;
        var before = diagnostics.All.Count;
        var unavailableCondition = UnavailableProductionDependencies.Condition(creation, body.Syntax, sources, body.Model, body.Scope.Bindings);
        var unavailableDestination = UnavailableProductionDependencies.Destination(creation, body.Syntax, sources, body.Model);
        var when = unavailableCondition is null ? _conditions.Resolve(creation, body.Syntax, body.Scope, type, request.Location) : null;
        var mappings = _mappings.Read(creation, body.Model, request.Command, type, request.Location, body.Scope.Bindings, sources).ToList();
        var generatedIdentity = body.Behavior is null && request.GeneratedIdentity;
        var production = new ProducesModel(type.Name, when, mappings)
        {
            EventTypeIdentity = EventProducers.IdentityOf(type),
            CodeMappedProperties = _mappings.CodeMappedProperties.ToArray(),
            UnavailableCondition = unavailableCondition,
            UnavailableDestination = unavailableDestination,
            UsesCommandContext = body.UsesCommandContext && new ReturnedEventDestinations(body.Model, body.Syntax, sources).Read(creation, body.Behavior is not null, generatedIdentity),
            CanInline = diagnostics.All.Count == before && when is null &&
                InlineProductionShape.IsUnconditional(body.Syntax) && InlineProductionShape.IsSupported(creation, body.Model)
        };

        return body.Behavior is null && Types is not null
            ? new ReturnedProductionRoutes(models, Types) { Definitions = Definitions }.Apply(production, creation, body.Syntax, body.Model, request.Command, sources, generatedIdentity)
            : (Production: production, UnsafeReturnedTupleWrapper: false);
    }

    /// <summary>
    /// Reads what a behavior of an aggregate root the handler reached produces.
    /// </summary>
    /// <param name="request">What the read is collecting into.</param>
    /// <param name="behavior">The behavior the handler reached.</param>
    /// <param name="usesCommandContext">Whether the reached aggregate belongs to the command context.</param>
    /// <remarks>
    /// The behavior is written wherever the aggregate root is, and a project that was not handed over is one whose
    /// source cannot be read at all. Saying so is the difference between a command stated as producing nothing and a
    /// reader who knows a project is missing from what the document was generated from.
    /// </remarks>
    void ReadBehavior(ProducesReadRequest request, AggregateRootInvocation behavior, bool usesCommandContext)
    {
        if (models.For(behavior.Body.SyntaxTree) is { } semanticModel)
        {
            ReadBody(request, BodyOf(request, behavior.Body, semanticModel, behavior) with { UsesCommandContext = usesCommandContext });

            return;
        }

        diagnostics.Warning(
            ScreenplayDiagnosticCodes.UnmappableCommandProduction,
            $"The command hands its work to '{behavior.AggregateRoot.Name}', which is written in a project the document was not generated from, so what it applies is not stated",
            request.Location);
    }

    /// <summary>
    /// Reports a handler yielding the identifier of the event source it appends to.
    /// </summary>
    /// <param name="command">The command declaring the handler.</param>
    /// <param name="handler">The handler to check.</param>
    /// <param name="location">Where the command lives.</param>
    void ReportEventSourceIdResult(INamedTypeSymbol command, IMethodSymbol handler, string location)
    {
        if (HandlerBodies.YieldsEventSourceId(handler.ReturnType))
        {
            var message = authoringOnlyConstructs
                ? $"The handler of command '{command.Name}' yields an event source identifier or response alongside the event, but its destination could not be proven; no event source destination was inferred"
                : $"The handler of command '{command.Name}' yields an event source identifier or response alongside the event, but its destination could not be proven as an admitted generated UUID concept; no event source destination was inferred";
            diagnostics.Information(ScreenplayDiagnosticCodes.UnmappableEventSourceIdResult, message, $"{location}.{command.Name}");
        }
    }
}
