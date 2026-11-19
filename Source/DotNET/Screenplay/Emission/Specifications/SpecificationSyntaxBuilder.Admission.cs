// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Specifications;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission.Specifications;

public partial class SpecificationSyntaxBuilder
{
    static bool HasUnrepresentableCommandSources(SpecificationScenarioEvidence? evidence, CommandModel? command, SpecificationModel specification)
    {
        if (evidence is not { HasExplicitCommandSources: true })
        {
            return false;
        }
        if (evidence.HasUnresolvedCommandSources)
        {
            return true;
        }
        if (command?.Authoring?.GeneratedParameters == true && specification.When?.For is not null)
        {
            return false;
        }

        return HasDifferentCommandIdentifier(evidence.CommandIdentifier, command);
    }

    static bool HasDifferentCommandIdentifier(string? statedIdentifier, CommandModel? command)
    {
        if (command is null || (command.Authoring?.Identifier ?? command.Identifier) is not { } identifier)
        {
            return false;
        }
        if (!command.Properties.Concat(command.Authoring?.Generated ?? []).Any(property => property.Name == identifier))
        {
            return false;
        }

        return command.Produces.All(production => production.UsesCommandContext) && statedIdentifier != identifier;
    }

    static TypeReferenceModel? IdentifierTypeOf(CommandModel command) =>
        command.Properties.Concat(command.Authoring?.Generated ?? []).SingleOrDefault(property => property.Name == (command.Authoring?.Identifier ?? command.Identifier))?.Type;

    static bool HasRoutedProductions(CommandModel? command, SpecificationModel specification) =>
        command?.Produces.Any(production => production.Route is not null) == true && !specification.Errors.Any();

    static CommandRouteModel? ContextStreamRoute(CommandModel? command) =>
        command is { Authoring.Route: { Stream: not null } route } && command.Produces.All(production => production.UsesCommandContext) ? route : null;

    static bool HasUnconcreteStreamId(CommandRouteModel route, LiteralSource? streamId, List<PropertyMappingModel> parts) =>
        (route.StreamIdType is not null && streamId is null) || parts.Exists(part => part.Source is not LiteralSource { Value: not null });

    static bool CannotDropSources(SpecificationModel specification, List<SpecificationStateModel> occurrences) =>
        specification.When is { Kind: SpecificationStateKind.Command } ||
        occurrences.Exists(state => state.Route is { Source: not null }) ||
        occurrences.Select(state => state.For).Distinct().Count() != 1;

    static bool Responds(CommandModel? command) => command?.Authoring is { Response: not null } or { ResponseFields.Count: > 0 };

    static IEnumerable<SpecificationStateModel> StatedStates(SpecificationModel specification) =>
        specification.Given.Concat(specification.Then).Concat(specification.When is { } action ? [action] : []);

    static List<SpecificationStateModel> EventOccurrences(SpecificationModel specification) =>
        StatedStates(specification).Where(state => state.Kind == SpecificationStateKind.Event).ToList();

    static bool RequiresLiteralRoute(SpecificationModel specification) =>
        specification.Given.Concat(specification.When is { } appendedAction ? [appendedAction] : [])
            .Any(state => state.Route is { Source: not null } && state.For is null);

    static MappingSourceModel? StatedWhenValue(SpecificationModel specification, string property) =>
        specification.When!.Values.SingleOrDefault(mapping => mapping.Property == property)?.Source;

    static LiteralSource? StatedStreamId(SpecificationModel specification, CommandRouteModel route) =>
        route.StreamId is { } property ? StatedWhenValue(specification, property) as LiteralSource : route.StreamIdLiteral;

    static MappingSourceModel StatedPartSource(SpecificationModel specification, MappingSourceModel value) => value switch
    {
        LiteralSource literal => literal,
        PropertyPathSource path => StatedWhenValue(specification, path.Path) ?? new LiteralSource(null),
        _ => new LiteralSource(null)
    };

    static List<PropertyMappingModel> StatedStreamIdParts(SpecificationModel specification, CommandRouteModel route) =>
        route.StreamIdParts.Select(part => new PropertyMappingModel(part.Name, StatedPartSource(specification, part.Value))).ToList();

    static SpecificationModel WithoutSources(SpecificationModel specification) => specification with
    {
        Given = specification.Given.Select(state => state with { For = null }).ToList(),
        When = specification.When is { } when ? when with { For = null } : null,
        Then = specification.Then.Select(state => state with { For = null }).ToList()
    };

    CommandModel? IssuedCommand(SpecificationModel specification) => specification.When is { Kind: SpecificationStateKind.Command } action
        ? Application?.Slices.SelectMany(slice => slice.Commands).FirstOrDefault(command => command.Name == action.Name)
        : null;

    string? LocationOf(SpecificationModel specification) => LocationOf(specification, SpecificationEvidence.For(specification));

    string? LocationOf(SpecificationModel specification, SpecificationScenarioEvidence? evidence) =>
        evidence?.SourceType.ToDisplayString() ??
            Application?.Slices.FirstOrDefault(slice => slice.Specifications.Contains(specification))?.Namespace;

    SpecificationModel? WithRepresentableSources(SpecificationModel specification)
    {
        var evidence = SpecificationEvidence.For(specification);
        var context = new AdmissionContext(IssuedCommand(specification), evidence, LocationOf(specification, evidence));
        foreach (var step in AdmissionSteps())
        {
            if (step(specification, context) is not { } admitted)
            {
                return null;
            }

            specification = admitted;
        }

        return specification;
    }

    Func<SpecificationModel, AdmissionContext, SpecificationModel?>[] AdmissionSteps() =>
    [
        RequireEmittedCommand,
        ApplyGenerationPins,
        ApplyProductionRoutes,
        RequireRepresentableCommandSources,
        InferCommandStream,
        RequireFittingResponse,
        RequireProvableResponseOnly,
        DropUnemittedResponse,
        RequireGeneratedFixtures,
        RequireRoutesAllowed,
        RequireStatableRoutes,
        ReconcileOccurrenceSources
    ];

    SpecificationModel? LeaveOut(SpecificationModel specification, string reason, string? location)
    {
        Diagnostics?.Warning(ScreenplayDiagnosticCodes.UnreadableSpecification, $"The scenario '{specification.Name}' was left out because {reason}", location);
        return null;
    }

    SpecificationModel? LeaveOutWithInformation(SpecificationModel specification, string reason, string? location)
    {
        Diagnostics?.Information(ScreenplayDiagnosticCodes.SpecificationRouteNotRepresentable, $"The scenario '{specification.Name}' was left out because {reason}", location);
        return null;
    }

    SpecificationModel? RequireEmittedCommand(SpecificationModel specification, AdmissionContext context) =>
        specification.When is { Kind: SpecificationStateKind.Command } issued &&
        Application?.Slices.SelectMany(slice => slice.Commands).Any(command => command.Name == issued.Name) == false
            ? LeaveOut(specification, "its command is not present in the emitted executable document", context.Location)
            : specification;

    SpecificationModel? ApplyGenerationPins(SpecificationModel specification, AdmissionContext context) =>
        new SpecificationGenerationPins(Diagnostics).Apply(specification, context.Command, context.Location);

    SpecificationModel? ApplyProductionRoutes(SpecificationModel specification, AdmissionContext context)
    {
        if (!EventRoutes || !HasRoutedProductions(context.Command, specification))
        {
            return specification;
        }

        return SpecificationProductionRoutes.Apply(specification, context.Command!) ??
            LeaveOut(specification, "its per-production destinations and effective routes are not complete ordered concrete fixtures", context.Location);
    }

    SpecificationModel? RequireRepresentableCommandSources(SpecificationModel specification, AdmissionContext context) =>
        HasUnrepresentableCommandSources(context.Evidence, context.Command, specification)
            ? LeaveOut(specification, "its event sources cannot be stated faithfully: an explicit source is not provably the command's own source and cannot be stated as a concrete for value", context.Location)
            : specification;

    SpecificationModel? InferCommandStream(SpecificationModel specification, AdmissionContext context)
    {
        if (!EventRoutes || ContextStreamRoute(context.Command) is not { } commandRoute)
        {
            return specification;
        }

        var streamId = StatedStreamId(specification, commandRoute);
        var streamIdParts = StatedStreamIdParts(specification, commandRoute);
        if (HasUnconcreteStreamId(commandRoute, streamId, streamIdParts))
        {
            return LeaveOut(specification, "its command's stream id is not a concrete fixture", context.Location);
        }

        return specification with
        {
            Then = specification.Then.Select(state => state.Kind == SpecificationStateKind.Event && state.For is null
                ? state with { Route = SpecificationRouteDeclarations.Inferred(commandRoute, streamId, streamIdParts) }
                : state).ToList()
        };
    }

    SpecificationModel? RequireFittingResponse(SpecificationModel specification, AdmissionContext context) =>
        specification.AssertsResponse && Responds(context.Command) && specification.Returns?.Fits(context.Command!.Authoring) != true
            ? LeaveOut(specification, "it asserts CommandResult.Response, but not as unconditional equalities with concrete values that fit the emitted returns", context.Location)
            : specification;

    SpecificationModel? RequireProvableResponseOnly(SpecificationModel specification, AdmissionContext context)
    {
        if (!ResponseOnlyScenarios.IsResponseOnly(specification))
        {
            return specification;
        }

        var responds = Responds(context.Command);
        if (responds && ResponseOnlyScenarios.RecordsNoFacts(context.Command))
        {
            return specification;
        }

        var reason = responds
            ? "its only outcome is the command response, and the command is not proven to record no facts; a specification with no expected events asserts that none were produced"
            : "its only outcome is a command response the emitted command does not return";

        return LeaveOut(specification, reason, context.Location);
    }

    /// <summary>
    /// Drops the response a scenario states when the command does not emit one, as in the legacy document.
    /// </summary>
    /// <param name="specification">The scenario.</param>
    /// <param name="context">The admission context.</param>
    /// <returns>The scenario without its unemitted response.</returns>
    SpecificationModel? DropUnemittedResponse(SpecificationModel specification, AdmissionContext context) =>
        specification.Returns is not null && !Responds(context.Command) ? specification with { Returns = null } : specification;

    SpecificationModel? RequireGeneratedFixtures(SpecificationModel specification, AdmissionContext context) =>
        !specification.Errors.Any() && context.Command?.Authoring is { GeneratedParameters: false, Generated.Count: > 0 }
            ? LeaveOut(specification, "generation needs deterministic when for / generated fixtures, and no faithful fixture was recovered from the Arc scenario; missing fixtures would execute as Unsupported(IdentityAllocation)", context.Location)
            : specification;

    SpecificationModel? RequireRoutesAllowed(SpecificationModel specification, AdmissionContext context) =>
        !EventRoutes && StatedStates(specification).Any(state => state.Route is { Source: not null })
            ? LeaveOutWithInformation(specification, "specification event routes require ScreenplayOptions.AuthoringOnlyConstructs; no stream metadata was discarded", context.Location)
            : specification;

    SpecificationModel? RequireStatableRoutes(SpecificationModel specification, AdmissionContext context)
    {
        if (!EventRoutes)
        {
            return specification;
        }

        var occurrences = EventOccurrences(specification);
        if (occurrences.Exists(state => state.Route?.Source == "Default"))
        {
            return LeaveOutWithInformation(specification, "source 'Default' has a reserved stored name (PLAY0273); no partial route was emitted", context.Location);
        }

        return RequiresLiteralRoute(specification) || occurrences.Exists(state => !CanStateRoute(state, occurrences))
            ? LeaveOut(specification, "its route cannot be stated with the document's source and stream-id types", context.Location)
            : specification;
    }

    SpecificationModel? ReconcileOccurrenceSources(SpecificationModel specification, AdmissionContext context)
    {
        var occurrences = EventOccurrences(specification);
        if (occurrences.TrueForAll(state => state.For is null || CanStateSource(state)))
        {
            return specification;
        }

        if (CannotDropSources(specification, occurrences))
        {
            return LeaveOut(specification, "its event sources cannot be stated faithfully as concrete for values of every producing command's unambiguous required scalar identifier type", context.Location);
        }

        return WithoutSources(specification);
    }

    sealed record AdmissionContext(CommandModel? Command, SpecificationScenarioEvidence? Evidence, string? Location);
}
