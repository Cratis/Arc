// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using Cratis.Arc.Screenplay.Analysis.Specifications;
using Cratis.Arc.Screenplay.Emission.Expressions;
using Cratis.Arc.Screenplay.Emission.Naming;
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
    /// Builds the specifications of a slice.
    /// </summary>
    /// <param name="specifications">The scenarios the slice is specified by.</param>
    /// <returns>The specifications, ordered by name.</returns>
    public IEnumerable<SpecificationSyntax> Build(IEnumerable<SpecificationModel> specifications) =>
    [
        .. specifications
            .Select(WithRepresentableSources)
            .OfType<SpecificationModel>()
            .Select(WithFollowingAssertions)
            .OfType<SpecificationModel>()
            .Select(Build)
            .OrderBy(_ => _.Name, StringComparer.Ordinal)
    ];

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d+)?)?(?:Z|[+-]\d{2}:\d{2})$", RegexOptions.None, 1000)]
    private static partial Regex IsoInstant();

    SpecificationModel? WithRepresentableSources(SpecificationModel specification)
    {
        var evidence = SpecificationEvidence.For(specification);
        var location = evidence?.SourceType.ToDisplayString() ??
            Application?.Slices.FirstOrDefault(slice => slice.Specifications.Contains(specification))?.Namespace;
        if (specification.When is { Kind: SpecificationStateKind.Command } issued &&
            Application?.Slices.SelectMany(slice => slice.Commands).Any(command => command.Name == issued.Name) == false)
        {
            Diagnostics?.Warning(ScreenplayDiagnosticCodes.UnreadableSpecification, $"The scenario '{specification.Name}' was left out because its command is not present in the emitted executable document", location);
            return null;
        }

        var command = specification.When is { Kind: SpecificationStateKind.Command } actionCommand
            ? Application?.Slices.SelectMany(slice => slice.Commands).FirstOrDefault(command => command.Name == actionCommand.Name)
            : null;
        if (evidence is { HasExplicitCommandSources: true } &&
            (evidence.HasUnresolvedCommandSources ||
             (command is not null && (command.Authoring?.Identifier ?? command.Identifier) is { } identifier &&
              command.Properties.Concat(command.Authoring?.Generated ?? []).Any(property => property.Name == identifier) &&
              command.Produces.All(production => production.UsesCommandContext) && evidence.CommandIdentifier != identifier)))
        {
            Diagnostics?.Warning(
                ScreenplayDiagnosticCodes.UnreadableSpecification,
                $"The scenario '{specification.Name}' was left out because its event sources cannot be stated faithfully: an explicit source is not provably the command's own source and cannot be stated as a concrete for value",
                location);
            return null;
        }

        var responds = command?.Authoring is { Response: not null } or { ResponseFields.Count: > 0 };
        if (specification.AssertsResponse && responds && specification.Returns?.Fits(command!.Authoring) != true)
        {
            Diagnostics?.Warning(ScreenplayDiagnosticCodes.UnreadableSpecification, $"The scenario '{specification.Name}' was left out because it asserts CommandResult.Response, but not as unconditional equalities with concrete values that fit the emitted returns", location);
            return null;
        }

        if (ResponseOnlyScenarios.IsResponseOnly(specification) && (!responds || !ResponseOnlyScenarios.RecordsNoFacts(command)))
        {
            var reason = !responds
                ? "its only outcome is a command response the emitted command does not return"
                : "its only outcome is the command response, and the command is not proven to record no facts; a specification with no expected events asserts that none were produced";
            Diagnostics?.Warning(ScreenplayDiagnosticCodes.UnreadableSpecification, $"The scenario '{specification.Name}' was left out because {reason}", location);
            return null;
        }

        if (specification.Returns is not null && !responds)
        {
            // As in the legacy document, a response the command does not emit is not asserted.
            specification = specification with { Returns = null };
        }

        if (!specification.Errors.Any() && command?.Authoring is { Generated.Count: > 0 })
        {
            Diagnostics?.Warning(
                ScreenplayDiagnosticCodes.UnreadableSpecification,
                $"The scenario '{specification.Name}' was left out because generation needs deterministic when for / generated fixtures, and no faithful fixture was recovered from the Arc scenario; missing fixtures would execute as Unsupported(IdentityAllocation)",
                location);
            return null;
        }

        var occurrences = specification.Given.Concat(specification.Then)
            .Concat(specification.When is { } action ? [action] : [])
            .Where(state => state.Kind == SpecificationStateKind.Event).ToList();
        if (occurrences.TrueForAll(state => state.For is null || CanStateSource(state)))
        {
            return specification;
        }

        if (specification.When is { Kind: SpecificationStateKind.Command } ||
            occurrences.Select(state => state.For).Distinct().Count() != 1)
        {
            Diagnostics?.Warning(
                ScreenplayDiagnosticCodes.UnreadableSpecification,
                $"The scenario '{specification.Name}' was left out because its event sources cannot be stated faithfully as concrete for values of every producing command's unambiguous required scalar identifier type",
                location);
            return null;
        }

        return specification with
        {
            Given = specification.Given.Select(state => state with { For = null }).ToList(),
            When = specification.When is { } when ? when with { For = null } : null,
            Then = specification.Then.Select(state => state with { For = null }).ToList()
        };
    }

    SpecificationModel? WithFollowingAssertions(SpecificationModel specification)
    {
        if (specification.When is not { Kind: SpecificationStateKind.Event })
        {
            return specification;
        }

        var remaining = specification.Then.Where(state => !SpecificationOutcomeReader.RestatesAppend(state, specification.When)).ToList();
        var location = SpecificationEvidence.For(specification)?.SourceType.ToDisplayString() ??
            Application?.Slices.FirstOrDefault(slice => slice.Specifications.Contains(specification))?.Namespace;
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

    bool CanStateSource(SpecificationStateModel state)
    {
        var producers = Application?.Slices.SelectMany(slice => slice.Commands)
            .Where(command => command.Produces.Any(produced => naming.ToDeclarationName(produced.EventName) == naming.ToDeclarationName(state.Name)))
            .ToList() ?? [];
        if (producers.Count == 0 || producers.Exists(command => (command.Authoring?.Identifier ?? command.Identifier) is null || command.Produces.Any(produced => !produced.UsesCommandContext)))
        {
            return false;
        }

        var identifiers = producers.ConvertAll(command => command.Properties.Concat(command.Authoring?.Generated ?? []).SingleOrDefault(property => property.Name == (command.Authoring?.Identifier ?? command.Identifier))?.Type);
        if (identifiers.Exists(type => type is null or { IsOptional: true } or { IsCollection: true }) ||
            identifiers.Select(type => naming.ToDeclarationName(type!.Name)).Distinct(StringComparer.Ordinal).Count() != 1)
        {
            return false;
        }

        var name = naming.ToDeclarationName(identifiers[0]!.Name);
        var concept = Application?.Concepts.SingleOrDefault(concept => naming.ToDeclarationName(concept.Name) == name);
        var primitive = concept?.Primitive.ToString() ?? name;
        var value = state.For!.Value;

        return primitive switch
        {
            "Uuid" => value is string uuid && Guid.TryParse(uuid, out var parsed) && parsed.ToString("D", CultureInfo.InvariantCulture) == uuid,
            "String" => value is string,
            "Int" => value is int or long,
            "Decimal" => value is int or long or float or double or decimal,
            "Bool" => value is bool,
            "Date" => value is string date && DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            "DateTime" => value is string instant && IsoInstant().IsMatch(instant) && DateTimeOffset.TryParse(instant, CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            "Enum" => value is string member && concept!.EnumValues.Contains(member, StringComparer.Ordinal),
            _ => false
        };
    }

    /// <summary>
    /// Builds one specification.
    /// </summary>
    /// <param name="specification">The scenario to build for.</param>
    /// <returns>The <see cref="SpecificationSyntax"/>.</returns>
    SpecificationSyntax Build(SpecificationModel specification) =>
        new(
            naming.ToDeclarationName(specification.Name),
            [.. Events(specification.Given)],
            When(specification.When),
            [.. Events(specification.Then)],
            [.. specification.Errors.Select(_ => new SpecificationErrorSyntax(naming.ToStringLiteral(_) ?? string.Empty, SourceLocation.Start))],
            SourceLocation.Start,
            [.. ReadModels(specification.Given)],
            [.. ReadModels(specification.Then)])
        {
            WhenAppended = specification.When is { Kind: SpecificationStateKind.Event } appended
                ? new(naming.ToDeclarationName(appended.Name), [.. Values(appended)], SourceLocation.Start) { For = SourceOf(appended) }
                : null,
            ThenReturns = Returns(specification.Returns)
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
    /// <param name="command">The command, or <see langword="null"/> when the scenario issued none.</param>
    /// <returns>The <see cref="SpecificationCommandSyntax"/>, or <see langword="null"/>.</returns>
    /// <remarks>
    /// A scenario about a read model issues no command - the events are what happened - and the language holds that
    /// as a specification with no <c>when</c> rather than as one with an empty one.
    /// </remarks>
    SpecificationCommandSyntax? When(SpecificationStateModel? command) =>
        command is { Kind: SpecificationStateKind.Command }
            ? new(naming.ToDeclarationName(command.Name), [.. Values(command)], SourceLocation.Start)
            : null;

    /// <summary>
    /// Builds the states of a step that name an event.
    /// </summary>
    /// <param name="states">The states to build from.</param>
    /// <returns>The events.</returns>
    IEnumerable<SpecificationEventSyntax> Events(IEnumerable<SpecificationStateModel> states) =>
        states
            .Where(_ => _.Kind == SpecificationStateKind.Event)
            .Select(_ => new SpecificationEventSyntax(naming.ToDeclarationName(_.Name), [.. Values(_)], SourceLocation.Start) { For = SourceOf(_) });

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
        state.Values.Select(_ => new PropertyMappingSyntax(naming.ToPropertyName(_.Property), _sources.Convert(_.Source), SourceLocation.Start));
}
