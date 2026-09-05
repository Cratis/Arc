// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Arc.Screenplay.Emission.Projections;

/// <summary>
/// Converts the join blocks of a projection scope.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
/// <param name="mappings">The <see cref="ProjectionMappingConverter"/> used for the mapping lines.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <param name="location">Where the projection lives, for use in diagnostics.</param>
/// <remarks>
/// The grammar names the property a whole join fills in and the property it keys on, and lists the events joined
/// from underneath, so every event sharing both properties has to be regrouped under a single block.
/// </remarks>
public class ProjectionJoinConverter(
    IScreenplayNaming naming,
    ProjectionMappingConverter mappings,
    ScreenplayDiagnostics diagnostics,
    string location)
{
    /// <summary>
    /// Converts the join blocks of a scope.
    /// </summary>
    /// <param name="joins">The blocks to convert.</param>
    /// <returns>The <see cref="JoinSyntax"/> blocks.</returns>
    public IEnumerable<JoinSyntax> Convert(IEnumerable<ProjectionJoinModel> joins)
    {
        var declared = joins as IReadOnlyCollection<ProjectionJoinModel> ?? [.. joins];

        foreach (var join in declared.Where(_ => _.On.Length == 0 || _.Property.Length == 0))
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableProjectionJoin,
                $"The join from '{join.EventType}' declares no property to hold the joined data, or no property to key on, and was left out",
                location);
        }

        return
        [
            .. declared
                .Where(_ => _.On.Length > 0 && _.Property.Length > 0)
                .GroupBy(_ => (_.Property, _.On))
                .Select(group => new JoinSyntax(
                    naming.ToPropertyName(group.Key.Property),
                    naming.ToPropertyName(group.Key.On),
                    [.. group.Select(ToJoinEvent)],
                    SourceLocation.Start))
        ];
    }

    /// <summary>
    /// Converts a single event joined from.
    /// </summary>
    /// <param name="join">The block to convert.</param>
    /// <returns>The <see cref="JoinEventSyntax"/>.</returns>
    JoinEventSyntax ToJoinEvent(ProjectionJoinModel join) =>
        new(
            naming.ToDeclarationName(join.EventType),
            AutoMapMode.Inherit,
            mappings.Convert(join.Properties, ReservedWords.None),
            SourceLocation.Start);
}
