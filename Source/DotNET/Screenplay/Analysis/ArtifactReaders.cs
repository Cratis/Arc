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
    public static ArtifactReaders For(Compilation compilation, ArtifactCatalog catalog, ScreenplayDiagnostics diagnostics)
    {
        var types = new TypeRegistry();
        var properties = new PropertyReader(types);
        var paths = SourcePaths.For(compilation, catalog);
        var aggregates = new AggregateRootCatalog();
        var produces = new ProducesReader(compilation, aggregates, diagnostics);
        var validators = ValidatorCatalog.From(catalog, new(compilation, diagnostics));

        return new(
            types,
            aggregates,
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
