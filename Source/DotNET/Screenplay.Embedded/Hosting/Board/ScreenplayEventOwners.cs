// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Holds the identity and shape of every event the application declares, keyed by its name.
/// </summary>
/// <remarks>
/// A slice that consumes an event has to reference the item the producing slice holds, and it is visited
/// before or after that slice with no ordering to rely on. Walking the declarations once up front - and
/// deriving the identities from the path rather than minting them - lets any slice reference any event.
/// </remarks>
public sealed class ScreenplayEventOwners
{
    readonly FrozenDictionary<string, Owner> _owners;

    ScreenplayEventOwners(IEnumerable<KeyValuePair<string, Owner>> owners) =>
        _owners = owners
            .GroupBy(owner => owner.Key, StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(group => group.Key, group => group.First().Value, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Walks everything the application declares and records the events it finds.
    /// </summary>
    /// <param name="documentId">The identifier of the document being read.</param>
    /// <param name="application">The compiled application.</param>
    /// <returns>The resulting <see cref="ScreenplayEventOwners"/>.</returns>
    public static ScreenplayEventOwners From(string documentId, ApplicationSyntax application) =>
        new(application.Modules?.SelectMany(module =>
            (module.Features ?? []).SelectMany(feature => ForFeature(documentId, module.Name, feature))) ?? []);

    /// <summary>
    /// Gets the identity of the item holding an event, or null when nothing declares it.
    /// </summary>
    /// <param name="name">The name of the event.</param>
    /// <returns>The identity, or null.</returns>
    public Guid? IdentityFor(string name) =>
        _owners.TryGetValue(name, out var owner) ? owner.Id : null;

    /// <summary>
    /// Gets the shape of an event, or an empty object schema when nothing declares it.
    /// </summary>
    /// <param name="name">The name of the event.</param>
    /// <returns>The schema.</returns>
    public JsonObject SchemaFor(string name) =>
        _owners.TryGetValue(name, out var owner)
            ? (JsonObject)owner.Schema.DeepClone()
            : SchemaSynthesizer.EmptyObjectSchema();

    static IEnumerable<KeyValuePair<string, Owner>> ForFeature(string documentId, string path, FeatureSyntax feature)
    {
        var featurePath = $"{path}/{feature.Name}";

        return (feature.Slices ?? [])
            .SelectMany(slice => (slice.Events ?? [])
                .Where(@event => !string.IsNullOrWhiteSpace(@event.Name))
                .Select(@event => new KeyValuePair<string, Owner>(
                    @event.Name,
                    new Owner(
                        DeterministicId.From(documentId, $"{featurePath}/{slice.Name}", "event", @event.Name),
                        @event.Properties.ToSchema()))))
            .Concat((feature.Features ?? []).SelectMany(subFeature => ForFeature(documentId, featurePath, subFeature)));
    }

    record Owner(Guid Id, JsonObject Schema);
}
