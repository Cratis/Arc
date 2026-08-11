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
/// <param name="recovered">The <see cref="RecoveredArtifacts"/> holding what each declaration yielded.</param>
/// <remarks>
/// A type can be more than one thing at once - a read model with queries is also the declaration of a projection -
/// so every recognizer is asked in turn rather than the first match winning.
/// </remarks>
public class SliceArtifactReader(ArtifactReaders readers, ScreenplayDiagnostics diagnostics, RecoveredArtifacts recovered)
{
    /// <summary>
    /// Reads everything one type contributes.
    /// </summary>
    /// <param name="type">The type to read.</param>
    /// <param name="namespace">The namespace the slice lives in.</param>
    /// <param name="content">The content collected so far.</param>
    /// <remarks>
    /// What the type yielded is taken as the difference it made to the content rather than recorded by each
    /// recognizer, so a recognizer added later is tied to its declaration without anyone remembering to say so.
    /// </remarks>
    public void Read(INamedTypeSymbol type, string @namespace, SliceContents content)
    {
        var before = content.Count;

        ReadInto(type, @namespace, content);

        recovered.Declare(type, content.Count - before);
    }

    /// <summary>
    /// Adds a projection the slice declares.
    /// </summary>
    /// <param name="content">The content collected so far.</param>
    /// <param name="projection">The projection to add, or <see langword="null"/> when nothing could be read.</param>
    /// <remarks>
    /// A slice declares as many projections as its behavior needs, so they accumulate rather than compete. The read
    /// model a screen binds to and the one a command reads to decide are two projections of one behavior, and a slice
    /// keeping only whichever was catalogued first stated one of them and silently dropped the rest.
    /// </remarks>
    static void Assign(SliceContents content, ProjectionModel? projection)
    {
        if (projection is not null)
        {
            content.Projections.Add(projection);
        }
    }

    /// <summary>
    /// Asks every recognizer what the type is.
    /// </summary>
    /// <param name="type">The type to read.</param>
    /// <param name="namespace">The namespace the slice lives in.</param>
    /// <param name="content">The content collected so far.</param>
    void ReadInto(INamedTypeSymbol type, string @namespace, SliceContents content)
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
            ReportWhatTheReadModelCannotSay(type, @namespace);
            AddQueries(content, type, QueryReader.MethodsOf(type), @namespace);
            Assign(content, readers.ModelBoundProjections.Read(type, @namespace));
        }

        if (FluentProjectionReader.ReadModelOf(type) is { } projected)
        {
            Assign(content, readers.FluentProjections.Read(type, projected, @namespace));
        }

        if (ReducerReader.ReadModelOf(type) is { } reduced)
        {
            Assign(content, readers.Reducers.Read(type, reduced, @namespace));
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

        ReportRoutes(ControllerRoutes.RoutesOf(type), $"The controller '{type.Name}'", @namespace);

        foreach (var method in ControllerRoutes.MethodsOf(type))
        {
            if (ControllerRoutes.IsCommand(method))
            {
                var command = readers.ControllerCommands.Read(type, method, @namespace);
                content.Commands.Add(command);
                ReportRoutes(ControllerRoutes.RoutesOf(method), $"The command '{command.Name}'", @namespace);
            }
            else if (ControllerRoutes.IsQuery(method))
            {
                AddQueries(content, type, [method], @namespace);
                ReportRoutes(ControllerRoutes.RoutesOf(method), $"The query '{method.Name}'", @namespace);
            }
        }
    }

    /// <summary>
    /// Reports everything a read model declares that the document has nowhere to hold.
    /// </summary>
    /// <param name="type">The read model.</param>
    /// <param name="location">Where it lives.</param>
    /// <remarks>
    /// A read model appears in the document only as the type a query answers with, so a tag on one has nowhere to go
    /// - while a tag on an event is printed. Saying so is what keeps a reader from taking the difference for the
    /// application's own.
    /// </remarks>
    void ReportWhatTheReadModelCannotSay(INamedTypeSymbol type, string location)
    {
        var tags = Tags.Of(type);
        if (tags.Length > 0)
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.ReadModelFeatureWithoutCounterpart,
                $"The read model '{type.Name}' is tagged {string.Join(", ", tags.Select(tag => $"'{tag}'"))}, and a read model is named in the document only as what a query answers with, so it has nowhere to carry them",
                location);
        }

        // A path on a query replaces the read model's outright rather than extending it, so the read model's own path is
        // only a route the application serves while some query still falls back to it. Reporting it regardless would
        // name a route nothing answers at.
        if (QueryReader.MethodsOf(type).Any(method => !method.HasAttribute(WellKnownTypeNames.PathAttribute)))
        {
            ReportRoutes([type.GetAttribute(WellKnownTypeNames.PathAttribute)?.GetArgument(0) as string], $"The read model '{type.Name}'", location);
        }
    }

    /// <summary>
    /// Reports each route the application serves an artifact at.
    /// </summary>
    /// <param name="routes">The routes that were declared, if any.</param>
    /// <param name="what">What declares them, as it reads at the start of the message.</param>
    /// <param name="location">Where it lives.</param>
    void ReportRoutes(IEnumerable<string?> routes, string what, string location)
    {
        foreach (var route in routes.Where(_ => !string.IsNullOrWhiteSpace(_)))
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.ServingConcernWithoutCounterpart,
                $"{what} is served at '{route}' rather than the conventional route, which Screenplay has no counterpart for",
                location);
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
}
