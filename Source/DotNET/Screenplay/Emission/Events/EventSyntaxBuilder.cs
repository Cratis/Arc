// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Types;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Events;

/// <summary>
/// Builds the Screenplay declaration for an event.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
/// <param name="types">The <see cref="TypeReferenceConverter"/> used for property types.</param>
/// <param name="names">The <see cref="NameAvailability"/> deciding which property names the body can carry.</param>
public class EventSyntaxBuilder(IScreenplayNaming naming, TypeReferenceConverter types, NameAvailability names)
{
    /// <summary>
    /// Builds the event declaration.
    /// </summary>
    /// <param name="event">The event to build for.</param>
    /// <param name="location">Where the event lives, for use in diagnostics.</param>
    /// <returns>The <see cref="EventSyntax"/>.</returns>
    public EventSyntax Build(EventModel @event, string location) =>
        new(
            naming.ToDeclarationName(@event.Name),
            [.. ToProperties(@event, location)],
            SourceLocation.Start,
            BuildTags(@event));

    /// <summary>
    /// Converts the properties of the event, leaving out every name the event body reads as a directive.
    /// </summary>
    /// <param name="event">The event to convert the properties of.</param>
    /// <param name="location">Where the event lives, for use in diagnostics.</param>
    /// <returns>The properties the body can carry.</returns>
    IEnumerable<PropertySyntax> ToProperties(EventModel @event, string location) =>
        @event.Properties
            .Where(_ => names.Allows(_.Name, ReservedWords.InEvent, @event.Name, location))
            .Select(ToProperty);

    /// <summary>
    /// Converts a property of the event.
    /// </summary>
    /// <param name="property">The property to convert.</param>
    /// <returns>The <see cref="PropertySyntax"/>.</returns>
    PropertySyntax ToProperty(PropertyModel property) =>
        new(naming.ToPropertyName(property.Name), types.Convert(property.Type), SourceLocation.Start);

    /// <summary>
    /// Builds the tags an event is classified by.
    /// </summary>
    /// <param name="event">The event to read.</param>
    /// <returns>The tags, or <see langword="null"/> when the event declares none.</returns>
    /// <remarks>
    /// A tag whose value is an identifier is printed bare, everything else through the expression renderer, so the
    /// value only has to survive as a string literal.
    /// </remarks>
    List<TagSyntax>? BuildTags(EventModel @event)
    {
        var tags = @event.Tags
            .Select(naming.ToStringLiteral)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(_ => new TagSyntax(new LiteralExpressionSyntax(_, SourceLocation.Start), SourceLocation.Start))
            .ToList();

        return tags.Count == 0 ? null : tags;
    }
}
