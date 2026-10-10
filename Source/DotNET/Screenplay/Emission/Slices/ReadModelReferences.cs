// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Commands;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission.Slices;

/// <summary>
/// Withholds references to placed read models whose declarations were omitted.
/// </summary>
internal static class ReadModelReferences
{
    /// <summary>
    /// Removes dependent declarations and screen directives with an explicit loss report.
    /// </summary>
    /// <param name="model">The application being emitted.</param>
    /// <param name="declared">The admitted read-model declarations.</param>
    /// <param name="diagnostics">The loss reports.</param>
    /// <returns>The application without references to omitted read models.</returns>
    internal static ApplicationModel Apply(ApplicationModel model, IReadOnlySet<ReadModelModel> declared, ScreenplayDiagnostics diagnostics)
    {
        var omitted = model.Slices.SelectMany(slice => slice.ReadModels.Where(readModel => !declared.Contains(readModel)).Concat(slice.OmittedReadModels))
            .Select(readModel => readModel.Name).ToHashSet(StringComparer.Ordinal);
        if (omitted.Count == 0)
        {
            return model;
        }

        return CommandProductionPruning.Complete(
            model,
            model with
            {
                Slices = model.Slices.Select(slice => Resolve(slice, model, omitted, diagnostics)).ToList()
            },
            diagnostics);
    }

    static SliceModel Resolve(SliceModel slice, ApplicationModel application, HashSet<string> omitted, ScreenplayDiagnostics diagnostics)
    {
        bool Keep(string readModel, string reference)
        {
            if (!omitted.Contains(readModel))
            {
                return true;
            }

            diagnostics.Information(
                ScreenplayDiagnosticCodes.UnmappableTypeReference,
                $"{reference} refers to omitted read model '{readModel}' and was left out",
                slice.Namespace);

            return false;
        }

        var queries = slice.Queries.Where(query => Keep(query.ReturnType.Name, $"Query '{query.Name}'")).ToList();
        var omittedQueries = slice.Queries.Except(queries).Select(query => query.Name).ToHashSet(StringComparer.Ordinal);
        var screens = slice.Screens.Select(screen => screen with
        {
            Data = screen.Data.Where(data => Keep(data.Type.Name, $"Screen '{screen.Name}' data '{data.Query}'") && !omittedQueries.Contains(data.Query)).ToList(),
            Tables = screen.Tables.Where(table => Keep(table.ReadModel, $"Screen '{screen.Name}' table '{table.ReadModel}'")).ToList()
        }).ToList();

        return slice with
        {
            Commands = slice.Commands.Select(command => AuthoringDeclarations.WithoutReadModels(command, application, omitted, diagnostics, slice.Namespace)).ToList(),
            Queries = queries,
            Screens = screens,
            Projections = slice.Projections.Where(projection => Keep(projection.ReadModel, $"Projection '{projection.Identifier}'")).ToList(),
            Specifications = slice.Specifications.Where(specification => specification.Given.Concat(specification.Then)
                .Where(state => state.Kind == SpecificationStateKind.ReadModel)
                .All(state => Keep(state.Name, $"Specification '{specification.Name}'"))).ToList()
        };
    }
}
