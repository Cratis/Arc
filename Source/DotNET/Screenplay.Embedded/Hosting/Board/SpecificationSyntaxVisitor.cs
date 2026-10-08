// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Visits a Screenplay specification and produces the specification the board draws under its slice.
/// </summary>
/// <param name="documentId">The identifier of the document being read.</param>
/// <param name="slicePath">The path to the slice holding the specification.</param>
/// <param name="owners">The events declared across the application.</param>
/// <param name="command">The command the board holds for the slice, when it holds one.</param>
/// <remarks>
/// A step's event points at the item declaring it, so the board draws it as that event. A State Change slice's
/// own command is drawn once by the board: a specification running it is that same card, with the values the
/// specification sets on it. An action that is not a command has one place on the board, so its kind is part
/// of its name - <c>append AuthorRegistered</c>, <c>clock 2026-10-05T08:00:00Z</c> - with its values carried as
/// a command's would be.
/// </remarks>
public class SpecificationSyntaxVisitor(string documentId, string slicePath, ScreenplayEventOwners owners, CommandItem? command)
    : ISpecificationSyntaxVisitor<SliceSpecification>
{
    /// <inheritdoc/>
    public SliceSpecification Visit(SpecificationSyntax syntax)
    {
        ArgumentNullException.ThrowIfNull(syntax);

        return new SliceSpecification(
            DeterministicId.From(documentId, slicePath, "specification", syntax.Name),
            SpecificationDetails.NameOf(syntax),
            [.. (syntax.Given ?? []).Select((@event, index) => Step(syntax, @event, "given", index))],
            Action(syntax),
            [.. (syntax.ThenEvents ?? []).Select((@event, index) => Step(syntax, @event, "then", index))],
            Errors(syntax),
            Collapsed: false);
    }

    Guid IdentityOf(SpecificationSyntax syntax, string kind, int index) =>
        DeterministicId.From(documentId, slicePath, "specification", syntax.Name, kind, index.ToString(System.Globalization.CultureInfo.InvariantCulture));

    SpecificationStep Step(SpecificationSyntax syntax, SpecificationEventSyntax @event, string kind, int index) => new(
        IdentityOf(syntax, kind, index),
        SpecificationDetails.StepName(@event),
        owners.IdentityFor(@event.EventType) ?? Guid.Empty,
        SpecificationValues.Of(@event.Values));

    List<SpecificationError> Errors(SpecificationSyntax syntax)
    {
        var errors = (syntax.ThenErrors ?? [])
            .Select((error, index) => new SpecificationError(
                IdentityOf(syntax, "error", index),
                string.IsNullOrWhiteSpace(error.Name) ? "error" : error.Name))
            .ToList();

        if (syntax.ThenDenied is not null)
        {
            errors.Add(new SpecificationError(IdentityOf(syntax, "denied", 0), "denied"));
        }

        return errors;
    }

    SpecificationAction? Action(SpecificationSyntax syntax)
    {
        var id = IdentityOf(syntax, "when", 0);
        if (syntax.When is { } when)
        {
            var commandId = command is not null && string.Equals(command.Name, when.CommandType, StringComparison.Ordinal) ? command.Id : (Guid?)null;
            return new SpecificationAction(id, commandId, when.CommandType, SpecificationValues.Of(when.Values));
        }

        var action = syntax switch
        {
            { WhenAppended: { } appended } => ($"append {SpecificationDetails.StepName(appended)}", SpecificationValues.Of(appended.Values)),
            { WhenClock: { } clock } => ($"clock {clock.Instant}", new JsonObject()),
            { WhenTrigger: { } trigger } => ($"trigger {trigger.Trigger}", SpecificationValues.Of(trigger.Values)),
            { WhenCapture: { } capture } => ($"capture {capture.Capture}", SpecificationValues.Of(capture.Record)),
            { WhenQuery: { } query } => ($"query {query.Query}", SpecificationValues.Of(query.Arguments)),
            _ => ((string Name, JsonObject Values)?)null
        };

        return action is { } named ? new SpecificationAction(id, CommandId: null, named.Name, named.Values) : null;
    }
}
