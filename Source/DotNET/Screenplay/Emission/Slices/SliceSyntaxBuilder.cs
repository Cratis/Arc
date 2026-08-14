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
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Arc.Screenplay.Emission.Slices;

/// <summary>
/// Builds the Screenplay <c>slice</c> declaration for a slice.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
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
    /// Builds the slice declaration.
    /// </summary>
    /// <param name="slice">The slice to build for.</param>
    /// <returns>The <see cref="SliceSyntax"/>.</returns>
    public SliceSyntax Build(SliceModel slice) =>
        new(
            SliceTypes.Convert(slice.Kind),
            GetName(slice),
            [.. slice.Events.Select(_ => events.Build(_, slice.Namespace)).OrderBy(_ => _.Name, StringComparer.Ordinal)],
            [.. slice.Commands.Select(_ => commands.Build(_, slice.Namespace)).OrderBy(_ => _.Name, StringComparer.Ordinal)],
            [.. slice.Queries.Select(queries.Build).OrderBy(_ => _.Name, StringComparer.Ordinal)],
            BuildProjections(slice),
            [],
            [
                .. slice.Reactors
                    .Select(_ => reactors.Build(_, slice.Namespace))
                    .OfType<ReactorSyntax>()
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
            naming.ToStringLiteral(slice.Description));

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
