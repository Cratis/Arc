// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Commands;
using Cratis.Arc.Screenplay.Emission.Constraints;
using Cratis.Arc.Screenplay.Emission.Events;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Projections;
using Cratis.Arc.Screenplay.Emission.Queries;
using Cratis.Arc.Screenplay.Emission.Reactors;
using Cratis.Arc.Screenplay.Emission.Screens;
using Cratis.Arc.Screenplay.Emission.Specifications;
using Cratis.Arc.Screenplay.Emission.Types;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Arc.Screenplay.Emission.Slices;

/// <summary>
/// Builds the Screenplay <c>slice</c> declaration for a slice.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
/// <param name="types">The shared converter for property types.</param>
/// <param name="commands">The <see cref="CommandSyntaxBuilder"/> for the commands of the slice.</param>
/// <param name="events">The <see cref="EventSyntaxBuilder"/> for the events of the slice.</param>
/// <param name="queries">The <see cref="QuerySyntaxBuilder"/> for the queries of the slice.</param>
/// <param name="constraints">The <see cref="ConstraintSyntaxBuilder"/> for the constraints of the slice.</param>
/// <param name="reactors">The <see cref="ReactorSyntaxBuilder"/> for the reactors of the slice.</param>
/// <param name="projections">The <see cref="ProjectionSyntaxBuilder"/> for the projection of the slice.</param>
/// <param name="screens">The <see cref="ScreenSyntaxBuilder"/> for the screens of the slice.</param>
/// <param name="specifications">The <see cref="SpecificationSyntaxBuilder"/> for the scenarios the slice is specified by.</param>
public class SliceSyntaxBuilder(
    IScreenplayNaming naming,
    TypeReferenceConverter types,
    CommandSyntaxBuilder commands,
    EventSyntaxBuilder events,
    QuerySyntaxBuilder queries,
    ConstraintSyntaxBuilder constraints,
    ReactorSyntaxBuilder reactors,
    ProjectionSyntaxBuilder projections,
    ScreenSyntaxBuilder screens,
    SpecificationSyntaxBuilder specifications)
{
    /// <summary>
    /// The name given to a slice whose own name yields nothing usable.
    /// </summary>
    public const string DefaultSliceName = "Slice";

    /// <summary>
    /// Gets the inline eligibility decisions shared with command emission.
    /// </summary>
    public InlineEvents? InlineEvents { get; init; }

    /// <summary>Gets or sets read-model declarations needed by authoring-only reads.</summary>
    public IReadOnlyList<CommandReadModel> AuthoringReadModels { get; set; } = [];

    /// <summary>
    /// Gets every read model the document declares, decided once for the whole document by
    /// <see cref="ReadModelDeclarations"/>.
    /// </summary>
    /// <remarks>
    /// A read model placed in a slice is declared only when it is among these, and an authoring-only <c>reads</c>
    /// declares a read model a second time only when it is not.
    /// </remarks>
    public IReadOnlySet<ReadModelModel> DeclaredReadModels { get; init; } = new HashSet<ReadModelModel>();

    /// <summary>
    /// Builds the slice declaration.
    /// </summary>
    /// <param name="slice">The slice to build for.</param>
    /// <returns>The <see cref="SliceSyntax"/>.</returns>
    public SliceSyntax Build(SliceModel slice)
    {
        var commandDeclarations = slice.Commands.Select(_ => commands.Build(_, slice.Namespace)).OrderBy(_ => _.Name, StringComparer.Ordinal).ToList();
        var inlineNames = commandDeclarations.SelectMany(_ => _.Produces).Select(_ => _.InlineEvent).OfType<EventSyntax>()
            .Select(_ => _.Name).ToHashSet(StringComparer.Ordinal);

        return new(
            SliceTypes.Convert(slice.Kind),
            GetName(slice),
            [.. slice.Events.Where(_ => InlineEvents?.Contains(_) != true || !inlineNames.Contains(naming.ToDeclarationName(_.Name))).Select(_ => events.Build(_, slice.Namespace)).OrderBy(_ => _.Name, StringComparer.Ordinal)],
            commandDeclarations,
            [.. slice.Queries.Select(queries.Build).OrderBy(_ => _.Name, StringComparer.Ordinal)],
            BuildProjections(slice),
            [],
            [
                .. slice.Reactors
                    .Select(_ => reactors.Build(_, slice.Namespace))
                    .OfType<ReactionSyntax>()
                    .OrderBy(_ => _.Name, StringComparer.Ordinal)
            ],
            [
                .. slice.Screens
                    .Select(_ => screens.Build(_, slice.Namespace))
                    .OrderBy(_ => _.Name, StringComparer.Ordinal)
                    .ThenBy(_ => _.File?.Path ?? string.Empty, StringComparer.Ordinal)
            ],
            [
                .. slice.Constraints
                    .Select(_ => constraints.Build(_, slice.Namespace))
                    .OrderBy(_ => _.Name, StringComparer.Ordinal)
            ],
            [.. specifications.Build(slice.Specifications)],
            SourceLocation.Start,
            naming.ToStringLiteral(slice.Description),
            ReadModels: [.. BuildReadModels(slice)]);
    }

    /// <summary>
    /// Builds the read models a slice declares.
    /// </summary>
    /// <param name="slice">The slice to build for.</param>
    /// <returns>The read models, ordered by name.</returns>
    /// <remarks>
    /// A read model placed in the slice is declared when the document declares it at all (see
    /// <see cref="DeclaredReadModels"/>). References to a placed read model the document leaves out are withheld
    /// together by <see cref="ReadModelReferences"/>. A read model an authoring-only <c>reads</c> needs, that the
    /// document declares nowhere, is declared in the slice of the projection building it.
    /// </remarks>
    IEnumerable<ReadModelSyntax> BuildReadModels(SliceModel slice)
    {
        var placed = slice.ReadModels
            .Where(DeclaredReadModels.Contains)
            .Select(readModel => new ReadModelSyntax(
                naming.ToDeclarationName(readModel.Name),
                [.. readModel.Properties.Select(ToProperty)],
                SourceLocation.Start,
                naming.ToStringLiteral(readModel.Description))
            {
                File = naming.ToFilePath(readModel.File) is { } path ? new FileReferenceSyntax(path, SourceLocation.Start) : null
            });
        var declared = DeclaredReadModels.Select(_ => _.Name).ToHashSet(StringComparer.Ordinal);
        var authoring = AuthoringReadModels
            .Where(read => read.Namespace == slice.Namespace && !declared.Contains(read.Name))
            .DistinctBy(read => read.Name)
            .Select(read => new ReadModelSyntax(naming.ToDeclarationName(read.Name), [.. read.Properties.Select(ToProperty)], SourceLocation.Start));

        return [.. placed.Concat(authoring).OrderBy(_ => _.Name, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Converts a property of a read model.
    /// </summary>
    /// <param name="property">The property to convert.</param>
    /// <returns>The <see cref="PropertySyntax"/>.</returns>
    PropertySyntax ToProperty(PropertyModel property) =>
        new(naming.ToPropertyName(property.Name), types.Convert(property.Type), SourceLocation.Start);

    /// <summary>
    /// Builds the projections a slice declares.
    /// </summary>
    /// <param name="slice">The slice to build for.</param>
    /// <returns>The projections, empty when the slice declares none.</returns>
    /// <remarks>
    /// A projection nothing could be expressed of yields nothing rather than an absent entry, so a slice left with
    /// no other content is still recognized as empty and dropped.
    /// </remarks>
    IEnumerable<ProjectionSyntax> BuildProjections(SliceModel slice) =>
    [
        .. slice.Projections
            .Select(_ => projections.Build(_, slice.Namespace))
            .OfType<ProjectionSyntax>()
            .OrderBy(_ => _.Name, StringComparer.Ordinal)
    ];

    /// <summary>
    /// Gets the name of the slice, falling back to the last segment of its namespace.
    /// </summary>
    /// <param name="slice">The slice to name.</param>
    /// <returns>The slice name.</returns>
    string GetName(SliceModel slice)
    {
        var name = naming.ToDeclarationName(slice.Name);
        if (name.Length > 1)
        {
            return name;
        }

        var segments = slice.Namespace.Split('.', StringSplitOptions.RemoveEmptyEntries);
        name = segments.Length == 0 ? string.Empty : naming.ToDeclarationName(segments[^1]);

        return name.Length <= 1 ? DefaultSliceName : name;
    }
}
