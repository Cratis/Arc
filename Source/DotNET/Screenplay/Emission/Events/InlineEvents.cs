// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission.Events;

/// <summary>
/// Decides once which slice-owned declarations may move into their sole command production.
/// </summary>
public class InlineEvents
{
    readonly Dictionary<string, EventModel> _declarations;
    readonly Dictionary<ProducesModel, EventModel> _productions = new(ReferenceEqualityComparer.Instance);
    readonly HashSet<EventModel> _events = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// Initializes a new instance of the <see cref="InlineEvents"/> class.
    /// </summary>
    /// <param name="model">The application, carrying its full producer census even in a scoped document.</param>
    public InlineEvents(ApplicationModel model) : this(model, new ScreenplayNaming())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InlineEvents"/> class with the emitted naming.
    /// </summary>
    /// <param name="model">The application, carrying its full producer census even in a scoped document.</param>
    /// <param name="naming">The naming used to check emitted property names.</param>
    public InlineEvents(ApplicationModel model, IScreenplayNaming naming)
    {
        _declarations = model.Slices.SelectMany(slice => slice.Events).Where(declaration => declaration.TypeIdentity is not null)
            .GroupBy(declaration => declaration.TypeIdentity!, StringComparer.Ordinal)
            .Where(group => group.Count() == 1).ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
        var productions = model.Slices.SelectMany(_ => _.Commands).SelectMany(_ => _.Produces).ToList();
        foreach (var slice in model.Slices)
        {
            foreach (var command in slice.Commands.Where(_ => HasIdentifier(_) && _.Produces.All(production => production.UsesCommandContext)))
            {
                foreach (var production in command.Produces.Where(_ => _.CanInline && _.UsesCommandContext && _.When is null))
                {
                    var declarations = slice.Events.Where(_ => _.TypeIdentity == production.EventTypeIdentity).ToList();
                    if (declarations is not [var declaration] || !declaration.CanInline ||
                        declaration.TypeIdentity is not { } key ||
                        !model.EventProducerCounts.TryGetValue(key, out var count) || count != 1 ||
                        productions.Count(_ => _.EventTypeIdentity == key) != 1 ||
                        !IsComplete(declaration, production, naming) || CopiesIdentifier(production, command.Authoring?.Identifier ?? command.Identifier!))
                    {
                        continue;
                    }

                    _productions.Add(production, declaration);
                    _events.Add(declaration);
                }
            }
        }
    }

    /// <summary>
    /// Gets the declaration selected for a production, or null for standalone syntax.
    /// </summary>
    /// <param name="production">The production.</param>
    /// <returns>The inline declaration, if eligible.</returns>
    public EventModel? For(ProducesModel production) => _productions.GetValueOrDefault(production);

    /// <summary>
    /// Gets whether the slice must omit the standalone declaration.
    /// </summary>
    /// <param name="event">The event declaration.</param>
    /// <returns>Whether the command declares this event inline.</returns>
    public bool Contains(EventModel @event) => _events.Contains(@event);

    /// <summary>
    /// Gets the standalone declaration whose property order also governs production mappings.
    /// </summary>
    /// <param name="production">The production.</param>
    /// <returns>The unambiguous event declaration, if known.</returns>
    internal EventModel? DeclarationFor(ProducesModel production) =>
        production.EventTypeIdentity is { } identity ? _declarations.GetValueOrDefault(identity) : null;

    static bool HasIdentifier(CommandModel command) =>
        (command.Authoring?.Identifier ?? command.Identifier) is { } identifier &&
        command.Properties.Concat(command.Authoring?.Generated ?? []).Any(_ => _.Name == identifier && !_.Type.IsOptional && !_.Type.IsCollection);

    static bool IsComplete(EventModel declaration, ProducesModel production, IScreenplayNaming naming)
    {
        var properties = declaration.Properties.Select(_ => naming.ToPropertyName(_.Name)).ToList();
        var mappings = production.Mappings.Select(_ => naming.ToPropertyName(_.Property)).ToList();

        return properties.Count == mappings.Count && properties.Distinct(StringComparer.Ordinal).Count() == properties.Count &&
            mappings.Distinct(StringComparer.Ordinal).Count() == mappings.Count &&
            properties.ToHashSet(StringComparer.Ordinal).SetEquals(mappings);
    }

    static bool CopiesIdentifier(ProducesModel production, string identifier) =>
        production.Mappings.Any(_ => _.Source is PropertyPathSource path &&
            (path.Path == identifier || path.Path.StartsWith($"{identifier}.", StringComparison.Ordinal)));
}
