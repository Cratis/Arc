// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Events;

/// <summary>
/// Reads the events a slice declares.
/// </summary>
/// <param name="properties">The <see cref="PropertyReader"/> reading the properties of each event.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// Generations, tombstones and compensations are all first class in Chronicle and have no counterpart in Screenplay
/// at all. Rather than inventing syntax for them, each is reported once for the event that declares it, so that a
/// reader of the document knows exactly what it does not say.
/// </remarks>
public class EventReader(PropertyReader properties, ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// The name of the argument carrying the generation of an event type.
    /// </summary>
    public const string GenerationArgument = "Generation";

    /// <summary>
    /// Determines whether a type is an event.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True when the type is an event.</returns>
    public static bool IsEvent(ITypeSymbol type) => type.HasAttribute(WellKnownTypeNames.EventTypeAttribute);

    /// <summary>
    /// Reads an event.
    /// </summary>
    /// <param name="type">The type declaring the event.</param>
    /// <param name="location">Where the event lives, for use in diagnostics.</param>
    /// <returns>The <see cref="EventModel"/>.</returns>
    public EventModel Read(INamedTypeSymbol type, string location)
    {
        ReportWhatIsLost(type, location);

        return new(type.Name, properties.Read(type), Tags(type));
    }

    /// <summary>
    /// Gets the tags an event is classified by.
    /// </summary>
    /// <param name="type">The type declaring the event.</param>
    /// <returns>The tags, ordered.</returns>
    static IEnumerable<string> Tags(ISymbol type) =>
    [
        .. type.GetAttributes()
            .Where(_ => _.AttributeClass.Is(WellKnownTypeNames.TagAttribute) || _.AttributeClass.Is(WellKnownTypeNames.TagsAttribute))
            .SelectMany(_ => _.ConstructorArguments)
            .SelectMany(_ => _.Kind == TypedConstantKind.Array ? _.Values.Select(value => value.Value) : [_.Value])
            .OfType<string>()
            .Where(_ => _.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
    ];

    /// <summary>
    /// Reports everything an event declares that the document cannot say.
    /// </summary>
    /// <param name="type">The type declaring the event.</param>
    /// <param name="location">Where the event lives.</param>
    void ReportWhatIsLost(INamedTypeSymbol type, string location)
    {
        var attribute = type.GetAttribute(WellKnownTypeNames.EventTypeAttribute);
        var generation = attribute?.GetNamedArgument(GenerationArgument) ?? attribute?.GetArgument(1);

        if (generation is uint declared && declared > 1)
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.EventFeatureWithoutCounterpart,
                $"The event '{type.Name}' is generation {declared}, and Screenplay describes only the current shape of an event",
                location);
        }

        if (type.HasAttribute(WellKnownTypeNames.TombstoneAttribute))
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.EventFeatureWithoutCounterpart,
                $"The event '{type.Name}' is a tombstone, which Screenplay has no counterpart for",
                location);
        }

        if (type.HasAttribute(WellKnownTypeNames.CompensationForAttribute) ||
            type.GetAttributes().Any(_ => _.AttributeClass.Is(WellKnownTypeNames.CompensationForAttributeOfT)))
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.EventFeatureWithoutCounterpart,
                $"The event '{type.Name}' compensates another event, which Screenplay has no counterpart for",
                location);
        }
    }
}
