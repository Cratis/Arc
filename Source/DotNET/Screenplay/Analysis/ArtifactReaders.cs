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
using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Analysis.Validation;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Composes every reader a slice needs, so that a slice reader takes one collaborator rather than nine.
/// </summary>
public class ArtifactReaders
{
    ArtifactReaders(
        TypeRegistry types,
        AggregateRootCatalog aggregates,
        ValidatorCatalog validators,
        EventReader events,
        CommandReader commands,
        ControllerCommandReader controllerCommands,
        QueryReader queries,
        ModelBoundProjectionReader modelBoundProjections,
        FluentProjectionReader fluentProjections,
        ReducerReader reducers,
        ReactorReader reactors,
        ConstraintReader constraints)
    {
        Types = types;
        AggregateRoots = aggregates;
        Validators = validators;
        Events = events;
        Commands = commands;
        ControllerCommands = controllerCommands;
        Queries = queries;
        ModelBoundProjections = modelBoundProjections;
        FluentProjections = fluentProjections;
        Reducers = reducers;
        Reactors = reactors;
        Constraints = constraints;
    }

    /// <summary>Gets the registry collecting the concepts every artifact refers to.</summary>
    public TypeRegistry Types { get; }

    /// <summary>Gets the aggregate roots the compilation declares, and which of them a command reaches.</summary>
    public AggregateRootCatalog AggregateRoots { get; }

    /// <summary>Gets the rules every validator in the compilation declares.</summary>
    public ValidatorCatalog Validators { get; }

    /// <summary>Gets the reader for events.</summary>
    public EventReader Events { get; }

    /// <summary>Gets the reader for model-bound commands.</summary>
    public CommandReader Commands { get; }

    /// <summary>Gets the reader for commands exposed by a controller.</summary>
    public ControllerCommandReader ControllerCommands { get; }

    /// <summary>Gets the reader for queries.</summary>
    public QueryReader Queries { get; }

    /// <summary>Gets the reader for projections declared with attributes.</summary>
    public ModelBoundProjectionReader ModelBoundProjections { get; }

    /// <summary>Gets the reader for projections defined against a builder.</summary>
    public FluentProjectionReader FluentProjections { get; }

    /// <summary>Gets the reader for reducers.</summary>
    public ReducerReader Reducers { get; }

    /// <summary>Gets the reader for reactors.</summary>
    public ReactorReader Reactors { get; }

    /// <summary>Gets the reader for constraints declared in code.</summary>
    public ConstraintReader Constraints { get; }

    /// <summary>
    /// Composes every reader for a compilation.
    /// </summary>
    /// <param name="compilation">The compilation being analyzed.</param>
    /// <param name="catalog">The catalogue of everything the compilation declares.</param>
    /// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
    /// <returns>The <see cref="ArtifactReaders"/>.</returns>
    public static ArtifactReaders For(Compilation compilation, ArtifactCatalog catalog, ScreenplayDiagnostics diagnostics) =>
        For(compilation, catalog, SourcePaths.For(compilation, catalog), WholeApplication.Of(compilation, diagnostics));

    /// <summary>
    /// Composes every reader for one project of an application, sharing what belongs to the application as a whole.
    /// </summary>
    /// <param name="compilation">The compilation being analyzed.</param>
    /// <param name="catalog">The catalogue of everything the compilation declares.</param>
    /// <param name="paths">The <see cref="SourcePaths"/> the paths of this project are written relative to.</param>
    /// <param name="whole">What the application as a whole holds.</param>
    /// <returns>The <see cref="ArtifactReaders"/>.</returns>
    /// <remarks>
    /// A concept is declared once at the top of a document however many projects refer to it, an aggregate root one
    /// project declares is handed its work by a command another project may hold, and the body of that aggregate
    /// root's behavior is read through the project it was written in rather than the one calling it. Everything else
    /// here reads a declaration the project itself catalogued, which is why there is a set of readers per project at
    /// all.
    /// </remarks>
    public static ArtifactReaders For(
        Compilation compilation,
        ArtifactCatalog catalog,
        SourcePaths paths,
        WholeApplication whole)
    {
        var types = whole.Types;
        var diagnostics = whole.Diagnostics;
        var properties = new PropertyReader(types);
        var produces = new ProducesReader(whole.Models, whole.AggregateRoots, diagnostics);
        var validators = ValidatorCatalog.From(catalog, new(compilation, diagnostics));

        return new(
            types,
            whole.AggregateRoots,
            validators,
            new EventReader(properties, diagnostics),
            new CommandReader(properties, produces, validators, paths),
            new ControllerCommandReader(types, properties, produces, validators, paths),
            new QueryReader(types, diagnostics),
            new ModelBoundProjectionReader(diagnostics),
            new FluentProjectionReader(compilation, diagnostics),
            new ReducerReader(diagnostics),
            new ReactorReader(compilation, paths),
            new ConstraintReader(compilation, paths, diagnostics));
    }
}
