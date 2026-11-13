// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Specifications;
using Cratis.Arc.Screenplay.Emission.Expressions;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Reactors;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Arc.Screenplay.Emission.Specifications;

/// <summary>
/// Builds the Screenplay <c>specification</c> blocks of a slice.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
/// <remarks>
/// Nothing a step states is ever left out over its name. Every other block decides what a line is from its first
/// word, so a property called after a directive collides with it; the values of a step are written one level deeper
/// than the step itself and the step takes every line beneath it as a value, whatever its first word says.
/// </remarks>
public partial class SpecificationSyntaxBuilder(IScreenplayNaming naming)
{
    readonly MappingSourceConverter _sources = new(naming);

    /// <summary>
    /// Gets the full application whose emitted command identifiers type occurrence sources.
    /// </summary>
    public ApplicationModel? Application { get; init; }

    /// <summary>
    /// Gets where scenarios with unrepresentable distinct sources are reported.
    /// </summary>
    public ScreenplayDiagnostics? Diagnostics { get; init; }

    /// <summary>
    /// Gets whether syntax-only event routes may be stated.
    /// </summary>
    public bool AuthoringOnlyConstructs { get; init; }

    /// <summary>
    /// Gets whether specification event routes are admitted by the executable version cap.
    /// </summary>
    public bool ExecutableRoutes { get; init; }

    /// <summary>
    /// Gets whether ESM v10 reaction metadata is admitted.
    /// </summary>
    public bool ReactionMetadataEnabled { get; init; } = true;

    /// <summary>
    /// Gets whether ESM v19 reaction refusal branches are admitted.
    /// </summary>
    public bool ReactionRefusalsEnabled { get; init; } = true;

    /// <summary>
    /// Gets where routed occurrences from retained specifications are collected for source declarations.
    /// </summary>
    public ICollection<SpecificationStateModel>? RoutedOccurrences { get; init; }

    /// <summary>
    /// Gets whether ESM v22 atomic reaction batches are admitted.
    /// </summary>
    internal bool ReactionAtomicBatchesEnabled { get; init; } = true;

    bool EventRoutes => AuthoringOnlyConstructs || ExecutableRoutes;

    /// <summary>
    /// Builds the specifications of a slice.
    /// </summary>
    /// <param name="specifications">The scenarios the slice is specified by.</param>
    /// <returns>The specifications, ordered by name.</returns>
    public IEnumerable<SpecificationSyntax> Build(IEnumerable<SpecificationModel> specifications)
    {
        var rows = specifications.Select(PrepareAndBuild).ToList();

        return FoldCases(rows).OrderBy(specification => specification.Name, StringComparer.Ordinal).ToList();
    }

    static SpecificationStateModel? ActionToAccountFor(SpecificationModel specification) =>
        specification.Errors.Any() || specification.Denied ? null : specification.When;

    /// <summary>
    /// Keeps a scenario only when what it says follows its action accounts for the declarative reactions it sets off.
    /// </summary>
    /// <param name="specification">The scenario.</param>
    /// <returns>The scenario, or <see langword="null"/> when it was left out.</returns>
    /// <remarks>
    /// Since ESM v6 the reactions a fact sets off run in a specification, and its <c>then</c> events are every fact
    /// that follows - the action's and the reactions'. A command or append scenario in Arc runs no reactor, so one
    /// whose facts set off a reaction the document states declaratively says less than the document would run, and is
    /// left out rather than stated as an outcome the reference would contradict. A reactor scenario is kept only while
    /// the reaction of its reactor is stated declaratively, is the only thing reacting to the appended event, and
    /// appends nothing that sets anything else off.
    /// </remarks>
    SpecificationModel? WithReactionsAccountedFor(SpecificationModel specification)
    {
        if (Application is null || ActionToAccountFor(specification) is not { } action)
        {
            return specification;
        }

        var reactions = new DeclarativeReactions(naming, Application) { MetadataEnabled = ReactionMetadataEnabled, RefusalsEnabled = ReactionRefusalsEnabled, AtomicBatchesEnabled = ReactionAtomicBatchesEnabled };
        var location = LocationOf(specification);
        var reason = specification.Reactor is { } reactor
            ? ReactorScenarioGap(specification, action, reactor, reactions)
            : ActionScenarioGap(specification, action, reactions);
        if (reason is null)
        {
            return specification;
        }

        Diagnostics?.Warning(ScreenplayDiagnosticCodes.UnreadableSpecification, $"The scenario '{specification.Name}' was left out because {reason}", location);
        return null;
    }

    string? ActionScenarioGap(SpecificationModel specification, SpecificationStateModel action, DeclarativeReactions reactions)
    {
        var stated = action.Kind == SpecificationStateKind.Event ? [action] : specification.Then.ToList();
        var facts = stated
            .Where(state => state.Kind == SpecificationStateKind.Event)
            .Select(state => naming.ToDeclarationName(state.Name))
            .ToHashSet(StringComparer.Ordinal);
        var reached = reactions.All().FirstOrDefault(item => facts.Contains(naming.ToDeclarationName(item.Reaction.EventName)));

        return reached.Reactor is null
            ? null
            : $"'{reached.Reaction.EventName}' follows its action and sets off the declarative reaction '{naming.ToDeclarationName(reached.Reactor.Name)}', which the scenario does not run, so what follows would not be what it states";
    }

    string? ReactorScenarioGap(SpecificationModel specification, SpecificationStateModel action, string reactor, DeclarativeReactions reactions)
    {
        var appended = naming.ToDeclarationName(action.Name);
        var reactors = Application!.Slices.SelectMany(slice => slice.Reactors).ToList();
        if (reactors.Where(model => model.Name == reactor).ToList() is not [var model] ||
            reactions.For(model, action.Name) is not { } reaction)
        {
            return $"the reaction of '{reactor}' to '{action.Name}' is not stated declaratively in the document";
        }

        if (model.EventSource is not null || model.ObservationFailure is not null)
        {
            return "its reactor observation filter requires a routed occurrence fixture that was not recovered; no unrestricted append was substituted";
        }

        if (reactors.Exists(other => !ReferenceEquals(other, model) && other.ObservedEvents.Any(observed => naming.ToDeclarationName(observed) == appended)))
        {
            return $"'{action.Name}' also sets off another reactor, whose effects the scenario does not state";
        }

        return ReactionOutcomeGap(specification, reaction, reactions);
    }

    string? ReactionOutcomeGap(SpecificationModel specification, ReactionModel reaction, DeclarativeReactions reactions)
    {
        var observed = reactions.Observed();
        if (reaction.Produces.Any(produced => observed.Contains(naming.ToDeclarationName(produced.EventName))))
        {
            return "an event its reaction appends sets off further reactions, whose effects the scenario does not state";
        }

        var expected = specification.Then.Select(state => naming.ToDeclarationName(state.Name)).Order(StringComparer.Ordinal);
        var produces = reaction.Produces.Select(produced => naming.ToDeclarationName(produced.EventName)).Order(StringComparer.Ordinal);

        return reaction.Invokes.Any() || !expected.SequenceEqual(produces, StringComparer.Ordinal)
            ? "it does not state every fact its reaction appends"
            : null;
    }

    SpecificationModel? WithFollowingAssertions(SpecificationModel specification)
    {
        if (specification.When is not { Kind: SpecificationStateKind.Event } || specification.Reactor is not null)
        {
            return specification;
        }

        var remaining = specification.Then.Where(state => !SpecificationOutcomeReader.RestatesAppend(state, specification.When)).ToList();
        var location = LocationOf(specification);
        if (remaining.Exists(state => state.Kind == SpecificationStateKind.Event))
        {
            Diagnostics?.Warning(
                ScreenplayDiagnosticCodes.UnreadableSpecification,
                $"The scenario '{specification.Name}' was left out because it expects another event type after the append, but no modeled reaction can produce it",
                location);
            return null;
        }

        if (remaining.Count == 0 && !specification.Errors.Any())
        {
            Diagnostics?.Warning(
                ScreenplayDiagnosticCodes.UnreadableSpecification,
                $"The scenario '{specification.Name}' was left out because its assertions only restate the appended fact; then events describe facts following the append, and no read-model, query, or error assertion remains",
                location);
            return null;
        }

        return specification with { Then = remaining };
    }

    /// <summary>
    /// Builds one specification.
    /// </summary>
    /// <param name="specification">The scenario to build for.</param>
    /// <returns>The <see cref="SpecificationSyntax"/>.</returns>
    SpecificationSyntax Build(SpecificationModel specification)
    {
        foreach (var occurrence in specification.Given.Concat(specification.Then).Concat(specification.When is { } action ? [action] : [])
            .Where(state => state.Route is { Source: not null, Stream: not null }))
        {
            RoutedOccurrences?.Add(occurrence);
        }

        return Syntax(specification);
    }

    /// <summary>
    /// Creates syntax without registering routed occurrences a second time when rows are folded.
    /// </summary>
    /// <param name="specification">The already admitted scenario.</param>
    /// <returns>The specification syntax.</returns>
    SpecificationSyntax Syntax(SpecificationModel specification) => new(
            naming.ToDeclarationName(specification.Name),
            [.. Events(specification.Given)],
            When(specification),
            [.. Events(specification.Then, expected: true)],
            [.. specification.Errors.Select(_ => new SpecificationErrorSyntax(naming.ToStringLiteral(_), SourceLocation.Start))],
            SourceLocation.Start,
            [.. ReadModels(specification.Given)],
            [.. ReadModels(specification.Then)])
    {
        WhenAppended = specification.When is { Kind: SpecificationStateKind.Event } appended
                ? Event(appended)
                : null,
        GivenCaller = Caller(specification.Caller),
        ThenReturns = Returns(specification.Returns),
        ThenDenied = specification.Denied ? new(SourceLocation.Start) : null,
        Parameters = CaseParameters(specification),
        Cases = CaseRows(specification)
    };

    /// <summary>
    /// Builds the response a scenario expects.
    /// </summary>
    /// <param name="returns">The expected response, or <see langword="null"/>.</param>
    /// <returns>The scalar or record subset expectation, or <see langword="null"/>.</returns>
    SpecificationReturnSyntax? Returns(SpecificationReturnModel? returns) => returns switch
    {
        { Value: { } value } => new ScalarSpecificationReturnSyntax(_sources.Convert(value), SourceLocation.Start),
        { Fields.Count: > 0 } => new RecordSpecificationReturnSyntax(
            [.. returns.Fields.Select(field => new PropertyMappingSyntax(naming.ToPropertyName(field.Property), _sources.Convert(field.Source), SourceLocation.Start))],
            SourceLocation.Start),
        _ => null
    };

    /// <summary>
    /// Builds the command a scenario issued.
    /// </summary>
    /// <param name="specification">The scenario whose command and generated fixtures are built.</param>
    /// <returns>The <see cref="SpecificationCommandSyntax"/>, or <see langword="null"/>.</returns>
    /// <remarks>
    /// A scenario about a read model issues no command - the events are what happened - and the language holds that
    /// as a specification with no <c>when</c> rather than as one with an empty one.
    /// </remarks>
    SpecificationCommandSyntax? When(SpecificationModel specification) =>
        specification.When is { Kind: SpecificationStateKind.Command } command
            ? new(naming.ToDeclarationName(command.Name), [.. Values(command)], SourceLocation.Start)
            {
                For = SourceOf(command),
                GeneratedValues = specification.GeneratedValues.Select(value => new PropertyMappingSyntax(naming.ToPropertyName(value.Property), _sources.Convert(value.Source), SourceLocation.Start)).ToList()
            }
            : null;

    /// <summary>
    /// Builds the states of a step that name an event.
    /// </summary>
    /// <param name="states">The states to build from.</param>
    /// <param name="expected">Whether these are then occurrences, where no stream is allowed.</param>
    /// <returns>The events.</returns>
    IEnumerable<SpecificationEventSyntax> Events(IEnumerable<SpecificationStateModel> states, bool expected = false) =>
        states
            .Where(_ => _.Kind == SpecificationStateKind.Event)
            .Select(state => Event(state, expected));

    SpecificationEventSyntax Event(SpecificationStateModel state, bool expected = false)
    {
        var unrouted = EventRoutes && state.Route is { Source: null, Stream: null };
        if (unrouted && !expected)
        {
            ReportUnroutedOccurrence(state);
        }

        return new(naming.ToDeclarationName(state.Name), [.. Values(state)], SourceLocation.Start)
        {
            For = SourceOf(state),
            Stream = EventRoutes ? StreamOf(state) : null,
            NoStream = unrouted && expected ? new(SourceLocation.Start) : null
        };
    }

    SpecificationStreamSyntax? StreamOf(SpecificationStateModel state) => state.Route is { Source: not null, Stream: not null } route
        ? new(route.Source, route.Stream, SourceLocation.Start)
        {
            StreamId = route.StreamId is { } id ? new("streamId", _sources.Convert(id), SourceLocation.Start) : null,
            StreamIdParts = StreamIdParts(route)
        }
        : null;

    void ReportUnroutedOccurrence(SpecificationStateModel state) =>
        Diagnostics?.Information(
            ScreenplayDiagnosticCodes.SpecificationRouteNotRepresentable,
            $"The unrouted occurrence of '{state.Name}' has no route directive because Screenplay allows no stream only on then events",
            state.Name);

    List<PropertyMappingSyntax> StreamIdParts(SpecificationEventRouteModel route) =>
        route.StreamIdParts.Select(part => new PropertyMappingSyntax(naming.ToPropertyName(part.Property), _sources.Convert(part.Source), SourceLocation.Start)).ToList();

    /// <summary>
    /// Builds the states of a step that name a read model.
    /// </summary>
    /// <param name="states">The states to build from.</param>
    /// <returns>The read models.</returns>
    IEnumerable<SpecificationReadModelSyntax> ReadModels(IEnumerable<SpecificationStateModel> states) =>
        states
            .Where(_ => _.Kind == SpecificationStateKind.ReadModel)
            .Select(_ => new SpecificationReadModelSyntax(naming.ToDeclarationName(_.Name), [.. Values(_)], SourceLocation.Start));

    /// <summary>
    /// Builds the concrete occurrence source, separately from payload values.
    /// </summary>
    /// <param name="state">The state to build from.</param>
    /// <returns>The source expression, or null when none is stated.</returns>
    ExpressionSyntax? SourceOf(SpecificationStateModel state) => state.For is { } source ? _sources.Convert(source) : null;

    /// <summary>
    /// Builds the values a step states.
    /// </summary>
    /// <param name="state">The state to build from.</param>
    /// <returns>The values.</returns>
    IEnumerable<PropertyMappingSyntax> Values(SpecificationStateModel state) =>
        state.Values.Select(_ => new PropertyMappingSyntax(naming.ToPropertyName(_.Property), FixtureValue(_.Source), SourceLocation.Start));

    ExpressionSyntax FixtureValue(MappingSourceModel source) => source is LiteralSource { Value: object?[] items }
        ? new ListExpressionSyntax(items.Select(item => FixtureValue(new LiteralSource(item))), SourceLocation.Start)
        : CaseValue(source);
}
