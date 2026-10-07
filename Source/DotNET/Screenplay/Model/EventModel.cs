// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents an immutable fact about something that happened.
/// </summary>
/// <param name="Name">The name of the event.</param>
/// <param name="Properties">The properties carried by the event.</param>
/// <param name="Tags">The tags the event is classified by.</param>
public record EventModel(string Name, IEnumerable<PropertyModel> Properties, IEnumerable<string> Tags)
{
    /// <summary>
    /// Gets the summary describing the event.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the remarks documenting the event in Markdown.
    /// </summary>
    public string? Documentation { get; init; }

    /// <summary>
    /// Gets the persisted name pinned by the event type attribute, when it differs from the type name.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Gets the assembly-qualified source type identity used by the producer census.
    /// </summary>
    public string? TypeIdentity { get; init; }

    /// <summary>
    /// Gets whether the analyzed declaration is local, generation one, and neither tombstone nor compensation.
    /// </summary>
    public bool CanInline { get; init; }
}
