// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    /// <summary>
    /// Builds the command declaration.
    /// </summary>
    /// <param name="command">The command to build for.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <returns>The <see cref="CommandSyntax"/>.</returns>
    public CommandSyntax Build(CommandModel command, string location)
    {
        var properties = ToProperties(command, location).ToList();
        var identifier = properties.Exists(_ => _.IsIdentifier) ? command.Identifier : null;
        var productions = command.Produces.ToList();
        if (productions.Exists(_ => !_.UsesCommandContext))
        {
            properties = [.. properties.Select(_ => _ with { IsIdentifier = false })];
        }

        var produced = produces.Build(productions, location, identifier).ToList();
        concurrency.ReportEventSource(command.EventSource, location);

        return new(
            naming.ToDeclarationName(command.Name),
            properties,
            authorize.Build(command.Authorization),
            [.. validations.Build(command.Validations, location)],
            produced,
            ToHandler(command, produced.Count),
            SourceLocation.Start,
            concurrency.Build(command.Concurrency, location),
            naming.ToStringLiteral(command.Description));
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
