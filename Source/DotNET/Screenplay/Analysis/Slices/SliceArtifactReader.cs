// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Aggregates;
using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Analysis.Constraints;
using Cratis.Arc.Screenplay.Analysis.Controllers;
using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Analysis.Projections;
using Cratis.Arc.Screenplay.Analysis.Queries;
using Cratis.Arc.Screenplay.Analysis.Reactors;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Slices;

/// <summary>
/// Reads everything one type contributes to the slice it lives in.
/// </summary>
/// <param name="readers">The <see cref="ArtifactReaders"/> reading each kind of artifact.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// A type can be more than one thing at once - a read model with queries is also the declaration of a projection -
/// so every recognizer is asked in turn rather than the first match winning.
/// </remarks>
public class SliceArtifactReader(ArtifactReaders readers, ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Reads everything one type contributes.
    /// </summary>
    /// <param name="type">The type to read.</param>
    /// <param name="namespace">The namespace the slice lives in.</param>
    /// <param name="content">The content collected so far.</param>
    public void Read(INamedTypeSymbol type, string @namespace, SliceContents content)
    {
        if (EventReader.IsEvent(type))
        {
            content.Events.Add(readers.Events.Read(type, @namespace));
            content.Constraints.AddRange(ConstraintReader.FromEvent(type));
        }

        if (CommandReader.IsCommand(type))
        {
            content.Commands.Add(readers.Commands.Read(type, @namespace));
        }

        if (QueryReader.IsReadModel(type))
        {
            AddQueries(content, type, QueryReader.MethodsOf(type), @namespace);
            Assign(content, readers.ModelBoundProjections.Read(type, @namespace), @namespace);
        }

        if (FluentProjectionReader.ReadModelOf(type) is { } projected)
        {
            Assign(content, readers.FluentProjections.Read(type, projected, @namespace), @namespace);
        }

        if (ReducerReader.ReadModelOf(type) is { } reduced)
        {
            Assign(content, readers.Reducers.Read(type, reduced, @namespace), @namespace);
        }

        if (ReactorReader.IsReactor(type))
        {
            content.Reactors.Add(readers.Reactors.Read(type));
        }

        if (ConstraintReader.IsConstraint(type))
        {
            content.Constraints.AddRange(readers.Constraints.Read(type, @namespace));
        }

        if (AggregateRoots.IsDeclaredByApplication(type))
        {
            content.HasAggregateRoot = true;
            readers.AggregateRoots.Declare(type, @namespace);
        }

        ReadController(type, @namespace, content);
    }

    /// <summary>
    /// Reads the commands and queries a controller exposes.
    /// </summary>
    /// <param name="type">The type to read.</param>
    /// <param name="namespace">The namespace the slice lives in.</param>
    /// <param name="content">The content collected so far.</param>
    void ReadController(INamedTypeSymbol type, string @namespace, SliceContents content)
    {
        if (!ControllerRoutes.IsController(type))
        {
            return;
        }

        foreach (var method in ControllerRoutes.MethodsOf(type))
        {
            if (ControllerRoutes.IsCommand(method))
            {
                content.Commands.Add(readers.ControllerCommands.Read(type, method, @namespace));
            }
            else if (ControllerRoutes.IsQuery(method))
            {
                AddQueries(content, type, [method], @namespace);
            }
        }
    }

    /// <summary>
    /// Reads the queries a type exposes, leaving out any whose read model cannot be named.
    /// </summary>
    /// <param name="content">The content collected so far.</param>
    /// <param name="declaring">The type declaring the queries.</param>
    /// <param name="methods">The methods exposing them.</param>
    /// <param name="namespace">The namespace the slice lives in.</param>
    void AddQueries(SliceContents content, INamedTypeSymbol declaring, IEnumerable<IMethodSymbol> methods, string @namespace)
    {
        foreach (var method in methods)
        {
            if (readers.Queries.Read(method, declaring, @namespace) is { } query)
            {
                content.Queries.Add(new(declaring.Name, query));
            }
        }
    }

    /// <summary>
    /// Assigns the single projection a slice may declare, reporting any beyond the first.
    /// </summary>
    /// <param name="content">The content collected so far.</param>
    /// <param name="projection">The projection to assign.</param>
    /// <param name="namespace">The namespace the slice lives in.</param>
    void Assign(SliceContents content, ProjectionModel? projection, string @namespace)
    {
        if (projection is null)
        {
            return;
        }

        if (content.Projection is not null)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableProjectionConstruct,
                $"'{projection.Identifier}' is a second projection in one slice, and a slice may declare at most one, so it was left out",
                @namespace);

            return;
        }

        content.Projection = projection;
    }
}
