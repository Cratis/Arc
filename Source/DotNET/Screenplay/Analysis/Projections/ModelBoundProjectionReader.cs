// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Projections;

/// <summary>
/// Reads the projection a model-bound read model declares through attributes.
/// </summary>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// Automatic property mapping is on unless the read model turns it off, which is the opposite of what a fluent
/// projection defaults to, so it is stated explicitly rather than inherited.
/// </remarks>
public class ModelBoundProjectionReader(ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// The identifier of the sequence a projection observes unless it says otherwise.
    /// </summary>
    public const string EventLogSequence = "event-log";

    /// <summary>
    /// Reads the projection a read model declares.
    /// </summary>
    /// <param name="readModel">The read model to read.</param>
    /// <param name="location">Where the read model lives, for use in diagnostics.</param>
    /// <returns>The <see cref="ProjectionModel"/>, or <see langword="null"/> when the read model declares none.</returns>
    public ProjectionModel? Read(INamedTypeSymbol readModel, string location)
    {
        var mappings = ModelBoundPropertyMappings.From(readModel);
        var from = ReadFrom(readModel, mappings).ToList();
        var removed = ReadRemovals(readModel, ModelBoundNames.RemovedWith).ToList();
        var removedViaJoin = ReadRemovals(readModel, ModelBoundNames.RemovedWithJoin).ToList();
        var joins = mappings.Joins.ToList();

        if (from.Count == 0 && removed.Count == 0 && removedViaJoin.Count == 0 && joins.Count == 0 && mappings.Every.Count == 0)
        {
            return null;
        }

        ReportWhatIsNotRead(readModel, location);

        var attribute = readModel.GetAttribute(WellKnownTypeNames.ProjectionAttribute);

        return new(
            attribute?.GetArgument(0) as string is { Length: > 0 } id ? id : readModel.ToDisplayString(),
            readModel.Name,
            attribute?.GetArgument(1) as string ?? EventLogSequence,
            readModel.HasAttribute(ModelBoundNames.NoAutoMap) ? ProjectionAutoMapMode.Disabled : ProjectionAutoMapMode.Enabled,
            readModel.HasAttribute(ModelBoundNames.FromAll),
            new(from, EveryOf(mappings), joins, [], [], removed, removedViaJoin));
    }

    /// <summary>
    /// Builds the block applying to every observed event.
    /// </summary>
    /// <param name="mappings">The mappings the read model declares.</param>
    /// <returns>The block, or <see langword="null"/> when the read model declares none.</returns>
    static ProjectionEveryModel? EveryOf(ModelBoundPropertyMappings mappings) =>
        mappings.Every.Count == 0 ? null : new(mappings.Every, true, ProjectionAutoMapMode.Inherit);

    /// <summary>
    /// Reads the blocks observing specific event types.
    /// </summary>
    /// <param name="readModel">The read model to read.</param>
    /// <param name="mappings">The mappings the read model declares.</param>
    /// <returns>The blocks, ordered by the event they observe.</returns>
    /// <remarks>
    /// An event named only by a property is observed just as much as one named by the read model itself, so both
    /// sources of event types are merged rather than the declaration having to say it twice.
    /// </remarks>
    static IEnumerable<ProjectionFromModel> ReadFrom(INamedTypeSymbol readModel, ModelBoundPropertyMappings mappings)
    {
        var declared = readModel.GetAttributes(ModelBoundNames.FromEvent)
            .Select(_ => (Name: EventTypeOf(_), Attribute: _))
            .Where(_ => _.Name is not null)
            .ToDictionary(_ => _.Name!, _ => _.Attribute, StringComparer.Ordinal);

        var names = declared.Keys.Concat(mappings.EventTypes()).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);

        return
        [
            .. names.Select(name => new ProjectionFromModel(
                [name],
                declared.TryGetValue(name, out var attribute) ? Argument(attribute, "Key", 0) : null,
                declared.TryGetValue(name, out var parent) ? Argument(parent, "ParentKey", 1) : null,
                mappings.For(name)))
        ];
    }

    /// <summary>
    /// Reads the blocks removing an instance when an event occurs.
    /// </summary>
    /// <param name="readModel">The read model to read.</param>
    /// <param name="attributeName">The fully qualified metadata name of the attribute declaring the removal.</param>
    /// <returns>The blocks, ordered by the event they observe.</returns>
    static IEnumerable<ProjectionRemoveModel> ReadRemovals(INamedTypeSymbol readModel, string attributeName) =>
    [
        .. readModel.GetAttributes(attributeName)
            .Select(_ => new ProjectionRemoveModel(EventTypeOf(_) ?? string.Empty, Argument(_, "Key", 0), Argument(_, "ParentKey", 1)))
            .Where(_ => _.EventType.Length > 0)
            .OrderBy(_ => _.EventType, StringComparer.Ordinal)
    ];

    /// <summary>
    /// Gets the name of the event type an attribute is bound to.
    /// </summary>
    /// <param name="attribute">The attribute to read.</param>
    /// <returns>The name, or <see langword="null"/> when the attribute names none.</returns>
    static string? EventTypeOf(AttributeData attribute) => attribute.AttributeClass?.TypeArguments.FirstOrDefault()?.Name;

    /// <summary>
    /// Gets an argument of an attribute, in either the named or the positional form.
    /// </summary>
    /// <param name="attribute">The attribute to read.</param>
    /// <param name="name">The name of the argument.</param>
    /// <param name="index">The position of the argument.</param>
    /// <returns>The value, or <see langword="null"/> when the argument carries nothing.</returns>
    static string? Argument(AttributeData attribute, string name, int index) =>
        (attribute.GetNamedArgument(name) ?? attribute.GetArgument(index)) as string is { Length: > 0 } value ? value : null;

    /// <summary>
    /// Reports the parts of a model-bound projection that are not read back.
    /// </summary>
    /// <param name="readModel">The read model to check.</param>
    /// <param name="location">Where the read model lives.</param>
    void ReportWhatIsNotRead(INamedTypeSymbol readModel, string location)
    {
        foreach (var property in readModel.DeclaredProperties())
        {
            if (property.HasAttribute(ModelBoundNames.ChildrenFrom) || property.HasAttribute(ModelBoundNames.Nested))
            {
                diagnostics.Warning(
                    ScreenplayDiagnosticCodes.UnmappableProjectionConstruct,
                    $"The child or nested object '{property.Name}' is declared with attributes, which source analysis does not read back yet, so it was left out",
                    location);
            }
        }
    }
}
