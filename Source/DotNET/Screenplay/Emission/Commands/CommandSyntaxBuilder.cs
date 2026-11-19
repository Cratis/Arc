// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Availability;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Policies;
using Cratis.Arc.Screenplay.Emission.Types;
using Cratis.Arc.Screenplay.Emission.Validation;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Commands;

/// <summary>
/// Builds the Screenplay <c>command</c> declaration for a command.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
/// <param name="types">The <see cref="TypeReferenceConverter"/> used for property types.</param>
/// <param name="authorize">The <see cref="AuthorizeSyntaxBuilder"/> used for the authorize block.</param>
/// <param name="validations">The <see cref="ValidationSyntaxBuilder"/> used for the validate blocks.</param>
/// <param name="produces">The <see cref="ProducesSyntaxBuilder"/> used for the produces blocks.</param>
/// <param name="concurrency">The <see cref="ConcurrencySyntaxBuilder"/> used for the concurrency block.</param>
/// <param name="names">The <see cref="NameAvailability"/> deciding which property names the body can carry.</param>
public class CommandSyntaxBuilder(
    IScreenplayNaming naming,
    TypeReferenceConverter types,
    AuthorizeSyntaxBuilder authorize,
    ValidationSyntaxBuilder validations,
    ProducesSyntaxBuilder produces,
    ConcurrencySyntaxBuilder concurrency,
    NameAvailability names)
{
    /// <summary>Gets whether authoring-only constructs should be emitted.</summary>
    public bool AuthoringOnlyConstructs { get; init; }

    /// <summary>
    /// Gets the builder of the <c>available</c> gates, when gates are stated.
    /// </summary>
    public AvailabilitySyntaxBuilder? Availability { get; init; }

    /// <summary>
    /// Builds the command declaration.
    /// </summary>
    /// <param name="command">The command to build for.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <returns>The <see cref="CommandSyntax"/>.</returns>
    public CommandSyntax Build(CommandModel command, string location)
    {
        var roots = command.Authoring?.Reads.Where(read => read.NamedByView).ToDictionary(read => naming.ToPropertyName(read.Alias), read => naming.ToDeclarationName(read.Name), StringComparer.Ordinal) ?? [];
        if (roots.Count == 0)
        {
            return BuildNamed(command, location);
        }

        var rooted = new ViewRootNaming(naming, roots);

        return new CommandSyntaxBuilder(rooted, types, authorize, validations, produces.WithNaming(rooted), concurrency, names) { AuthoringOnlyConstructs = AuthoringOnlyConstructs }.BuildNamed(command, location);
    }

    static bool HasUnroutedProduction(List<ProducesModel> productions) => productions.Exists(_ => !_.UsesCommandContext && _.Destination is null);

    static bool IsLegacyRoute(CommandModel command) => command.EventSource is null && command.HasAuthoringRoute;

    static bool IsUnroutedScope(CommandModel command) =>
        command.Authoring?.Route is null && command.Concurrency is not null && command.EventSource is null && !command.HasAuthoringRoute;

    CommandSyntax BuildNamed(CommandModel command, string location)
    {
        var authoring = command.Authoring;
        var selectedIdentifier = authoring?.Identifier ?? command.Identifier;
        var properties = ToAuthoredProperties(command, location, selectedIdentifier);

        var identifier = properties.Exists(_ => _.IsIdentifier) ? selectedIdentifier : null;
        var productions = command.Produces.ToList();
        if (HasUnroutedProduction(productions))
        {
            properties = [.. properties.Select(_ => _ with { IsIdentifier = false })];
        }

        var produced = produces.Build(productions, location, identifier, command.Name).ToList();
        ReportConcurrency(command, location);

        var syntax = new CommandSyntax(
            naming.ToDeclarationName(command.Name),
            properties,
            authorize.Build(command.Authorization),
            [.. validations.Build(command.Validations, location)],
            produced,
            AuthoringOnlyConstructs || !command.HasNoFactBehavior ? ToHandler(command, produced.Count) : null,
            SourceLocation.Start,
            !AuthoringOnlyConstructs && authoring?.Route is { Stream: not null } ? null : concurrency.Build(command.Concurrency, location),
            naming.ToStringLiteral(command.Description));

        var authored = new CommandAuthoringSyntaxBuilder(naming, types).Apply(syntax, authoring, AuthoringOnlyConstructs);

        return Availability is null ? authored : authored with { Available = [.. authored.Available, .. Availability.Build(command.Availability, $"{location}.{command.Name}")] };
    }

    /// <summary>
    /// Converts the properties of the command, marking the selected identifier and adding the generated values.
    /// </summary>
    /// <param name="command">The command to convert the properties of.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <param name="selectedIdentifier">The property selected as the command's identifier.</param>
    /// <returns>The properties the body carries.</returns>
    List<PropertySyntax> ToAuthoredProperties(CommandModel command, string location, string? selectedIdentifier)
    {
        var properties = ToProperties(command, location).ToList();
        if (command.Authoring is not { } authoring)
        {
            return properties;
        }

        return [.. properties.Select(property => property with { IsIdentifier = selectedIdentifier is not null && property.Name == naming.ToPropertyName(selectedIdentifier) }),
            .. authoring.Generated.Select(property => ToProperty(property, selectedIdentifier) with { IsGenerated = true })];
    }

    /// <summary>
    /// Reports what the command's routing and concurrency state leaves out of the declaration.
    /// </summary>
    /// <param name="command">The command to report for.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    void ReportConcurrency(CommandModel command, string location)
    {
        ReportRouting(command, location);
        if (!AuthoringOnlyConstructs)
        {
            ReportScope(command, $"{location}.{command.Name}");
        }
    }

    void ReportRouting(CommandModel command, string location)
    {
        if (command.Authoring?.Route is not null)
        {
            if (command.EventSource is { ConcurrentByStreamId: true })
            {
                concurrency.ReportStreamIdFlag(location);
            }

            return;
        }

        var commandLocation = $"{location}.{command.Name}";
        concurrency.ReportEventSource(command.EventSource, commandLocation, AuthoringOnlyConstructs, command.Name);
        if (!AuthoringOnlyConstructs && IsLegacyRoute(command))
        {
            concurrency.ReportLegacyRoute(commandLocation, command.Name);
        }
    }

    void ReportScope(CommandModel command, string commandLocation)
    {
        if (command.Authoring?.Route is { Stream: not null } && command.Concurrency is { } omittedScope)
        {
            concurrency.ReportOmittedScope(omittedScope, commandLocation);
        }

        if (concurrency.ExecutableRoutes && IsUnroutedScope(command))
        {
            concurrency.ReportLegacyScope(commandLocation);
        }
    }

    /// <summary>
    /// Converts the properties of the command, leaving out every name the command body reads as a directive.
    /// </summary>
    /// <param name="command">The command to convert the properties of.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <returns>The properties the body can carry.</returns>
    IEnumerable<PropertySyntax> ToProperties(CommandModel command, string location) =>
        command.Properties
            .Where(_ => names.Allows(_.Name, ReservedWords.InCommand, command.Name, location))
            .Select(property => ToProperty(property, command.Identifier));

    /// <summary>
    /// Converts a property of the command.
    /// </summary>
    /// <param name="property">The property to convert.</param>
    /// <param name="identifier">The property supplying the command's event source identity.</param>
    /// <returns>The <see cref="PropertySyntax"/>.</returns>
    PropertySyntax ToProperty(PropertyModel property, string? identifier) =>
        new(naming.ToPropertyName(property.Name), types.Convert(property.Type), SourceLocation.Start, property.Name == identifier);

    /// <summary>
    /// Builds the handler reference for a command whose behavior is not expressed declaratively.
    /// </summary>
    /// <param name="command">The command to build for.</param>
    /// <param name="producedCount">The number of produces blocks that were emitted.</param>
    /// <returns>The <see cref="HandlerSyntax"/>, or <see langword="null"/> when there is nothing to point at.</returns>
    /// <remarks>
    /// A command declaring both a handler and a produces block does not compile, so the file is only pointed at when
    /// nothing was produced declaratively.
    /// </remarks>
    HandlerSyntax? ToHandler(CommandModel command, int producedCount)
    {
        if (producedCount > 0 || naming.ToFilePath(command.SourceFilePath) is not { } path)
        {
            return null;
        }

        return new(new FileReferenceSyntax(path, SourceLocation.Start), null, SourceLocation.Start);
    }
}
