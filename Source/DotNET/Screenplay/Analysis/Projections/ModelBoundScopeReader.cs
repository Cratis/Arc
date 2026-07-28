// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Projections;

/// <summary>
/// Reads one scope of a model-bound projection - the read model itself, or a child or nested object within it.
/// </summary>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// A model-bound projection is declared upside down compared to a fluent one - the read model's properties say which
/// event they come from, rather than the event saying which properties it fills in. Regrouping them by event is what
/// turns the declaration back into the blocks a document is written in.
/// </remarks>
public class ModelBoundScopeReader(ScreenplayDiagnostics diagnostics)
{
    readonly ModelBoundChildren _children = new(diagnostics);

    /// <summary>
    /// Determines whether a scope declares nothing at all.
    /// </summary>
    /// <param name="scope">The scope to check.</param>
    /// <returns>True when there is nothing to express.</returns>
    public static bool IsEmpty(ProjectionScopeModel scope) =>
        !scope.From.Any() &&
        scope.Every is null &&
        !scope.Joins.Any() &&
        !scope.Children.Any() &&
        !scope.Nested.Any() &&
        !scope.RemovedWith.Any() &&
        !scope.RemovedWithJoin.Any();

    /// <summary>
    /// Reads everything a read model declares, following the types it holds.
    /// </summary>
    /// <param name="readModel">The read model to read.</param>
    /// <param name="location">Where the read model lives, for use in diagnostics.</param>
    /// <returns>The <see cref="ProjectionScopeModel"/>.</returns>
    public ProjectionScopeModel Read(INamedTypeSymbol readModel, string location) => Read(readModel, location, [readModel]);

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
            .Select(_ => (Name: ModelBoundAttributes.EventTypeOf(_), Attribute: _))
            .Where(_ => _.Name is not null)
            .ToDictionary(_ => _.Name!, _ => _.Attribute, StringComparer.Ordinal);

        var names = declared.Keys.Concat(mappings.EventTypes()).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);

        return
        [
            .. names.Select(name => new ProjectionFromModel(
                [name],
                declared.TryGetValue(name, out var attribute) ? ModelBoundAttributes.Path(attribute, "Key", 0) : null,
                declared.TryGetValue(name, out var parent) ? ModelBoundAttributes.Path(parent, "ParentKey", 1) : null,
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
            .Select(_ => new ProjectionRemoveModel(
                ModelBoundAttributes.EventTypeOf(_) ?? string.Empty,
                ModelBoundAttributes.Path(_, "Key", 0),
                ModelBoundAttributes.Path(_, "ParentKey", 1)))
            .Where(_ => _.EventType.Length > 0)
            .OrderBy(_ => _.EventType, StringComparer.Ordinal)
    ];

    /// <summary>
    /// Reads everything a read model declares, without following a type it is already within.
    /// </summary>
    /// <param name="readModel">The read model to read.</param>
    /// <param name="location">Where the read model lives, for use in diagnostics.</param>
    /// <param name="visited">The types the scope is already within.</param>
    /// <returns>The <see cref="ProjectionScopeModel"/>.</returns>
    ProjectionScopeModel Read(INamedTypeSymbol readModel, string location, IReadOnlyCollection<ISymbol> visited)
    {
        var mappings = ModelBoundPropertyMappings.From(readModel, diagnostics, location);

        var scope = new ProjectionScopeModel(
            ReadFrom(readModel, mappings),
            EveryOf(mappings),
            [.. mappings.Joins],
            ReadScopes(_children.In(readModel, location), location, visited),
            ReadScopes(_children.NestedIn(readModel, location), location, visited),
            ReadRemovals(readModel, ModelBoundNames.RemovedWith),
            ReadRemovals(readModel, ModelBoundNames.RemovedWithJoin));

        ReportWhatIsNotRead(readModel, scope, location);

        return scope;
    }

    /// <summary>
    /// Reads the child collections or nested objects of a scope.
    /// </summary>
    /// <param name="children">The children or nested objects declared.</param>
    /// <param name="location">Where the read model lives, for use in diagnostics.</param>
    /// <param name="visited">The types the scope is already within.</param>
    /// <returns>The scopes, in the order they were declared in.</returns>
    /// <remarks>
    /// A type holding itself describes a hierarchy of no fixed depth, which a document writes out one level at a
    /// time and therefore cannot express. The level that is expressible is kept and the rest is reported, rather
    /// than the whole child being dropped or the reader following it forever.
    /// </remarks>
    List<ProjectionChildScopeModel> ReadScopes(
        IEnumerable<ModelBoundChild> children,
        string location,
        IReadOnlyCollection<ISymbol> visited)
    {
        var scopes = new List<ProjectionChildScopeModel>();

        foreach (var child in children)
        {
            if (visited.Any(_ => SymbolEqualityComparer.Default.Equals(_, child.Type)))
            {
                diagnostics.Warning(
                    ScreenplayDiagnosticCodes.UnmappableProjectionScope,
                    $"'{child.Property}' holds objects of a type it is already within, which a document cannot nest without end, so what they contain was left out",
                    location);

                scopes.Add(child.ToScope(ProjectionScopeModel.Empty));
                continue;
            }

            scopes.Add(child.ToScope(Read(child.Type, location, [.. visited, child.Type])));
        }

        return scopes;
    }

    /// <summary>
    /// Reports the parts of a scope that are not read back, leaving a read model declaring no projection alone.
    /// </summary>
    /// <param name="readModel">The read model to check.</param>
    /// <param name="scope">What was read from it.</param>
    /// <param name="location">Where the read model lives.</param>
    void ReportWhatIsNotRead(INamedTypeSymbol readModel, ProjectionScopeModel scope, string location)
    {
        if (IsEmpty(scope))
        {
            return;
        }

        foreach (var attribute in readModel.GetAttributes(ModelBoundNames.ClearWith))
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableProjectionConstruct,
                $"'{readModel.Name}' is emptied again when '{ModelBoundAttributes.EventTypeOf(attribute)}' occurs, which the model a projection is built from carries nowhere, so it was left out",
                location);
        }
    }
}
