// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Expressions;
using Cratis.Arc.Screenplay.Emission.Files;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Reactors;

/// <summary>
/// Builds the Screenplay <c>reaction</c> declaration for a reactor.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// A handler whose whole effect is what it returns is stated as what it sets off - <c>produces</c> for the events it
/// appends and <c>invokes</c> for the commands it executes - so the reference evaluator runs it in specifications. Any
/// other handler is code, so the only faithful rendering of it is a file reference. A trigger with neither a body nor a
/// declared effect would say nothing, which is why a path is always resolved for the handlers that are code.
/// </remarks>
public class ReactorSyntaxBuilder(IScreenplayNaming naming, ScreenplayDiagnostics diagnostics)
{
    readonly MappingSourceConverter _sources = new(naming);

    /// <summary>
    /// Gets the application the document is written for, which decides what a declarative reaction can name.
    /// </summary>
    /// <remarks>
    /// Without it nothing is stated declaratively, because whether a reaction binds depends on the events and
    /// commands the document declares.
    /// </remarks>
    public ApplicationModel? Application { get; init; }

    /// <summary>
    /// Builds the reactor declaration.
    /// </summary>
    /// <param name="reactor">The reactor to build for.</param>
    /// <param name="namespace">The namespace of the slice the reactor lives in.</param>
    /// <returns>The <see cref="ReactionSyntax"/>, or <see langword="null"/> when it observes no events.</returns>
    public ReactionSyntax? Build(ReactorModel reactor, string @namespace)
    {
        new ObservedEventSourceReport(diagnostics).Report("reactor", reactor.Name, reactor.EventSource, @namespace);

        var name = naming.ToDeclarationName(reactor.Name);
        var path = naming.ToFilePath(reactor.SourceFilePath) ?? SourceFilePaths.Conventional(@namespace, name);
        var file = new FileReferenceSyntax(path, SourceLocation.Start);
        var declarative = Application is null ? null : new DeclarativeReactions(naming, Application);

        var triggers = reactor.ObservedEvents
            .Select(_ => (Event: _, Name: naming.ToDeclarationName(_)))
            .Where(_ => _.Name.Length > 1)
            .DistinctBy(_ => _.Name, StringComparer.Ordinal)
            .Select(_ => Trigger(_.Name, declarative?.For(reactor, _.Event), file))
            .ToList();

        if (triggers.Count == 0)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.ReactorWithoutEvents,
                $"The reactor '{reactor.Name}' observes no events and was left out",
                @namespace);

            return null;
        }

        return new(name, triggers, SourceLocation.Start);
    }

    /// <summary>
    /// Builds one trigger of the reaction.
    /// </summary>
    /// <param name="event">The declaration name of the observed event.</param>
    /// <param name="reaction">The declarative reaction to the event, or <see langword="null"/> when its handler is code.</param>
    /// <param name="file">The file the reactor is written in.</param>
    /// <returns>The <see cref="ReactionTriggerSyntax"/>.</returns>
    ReactionTriggerSyntax Trigger(string @event, ReactionModel? reaction, FileReferenceSyntax file) =>
        new(
            new NamedTriggerSourceSyntax(@event, SourceLocation.Start),
            [],
            reaction is null ? file : null,
            null,
            SourceLocation.Start,
            Produces: reaction is null ? null : [.. reaction.Produces.Select(Produces)],
            Invokes: reaction is null ? null : [.. reaction.Invokes.Select(Invokes)]);

    /// <summary>
    /// Builds an event a reaction appends to the event source of the event that set it off.
    /// </summary>
    /// <param name="produces">The production.</param>
    /// <returns>The <see cref="ProducesSyntax"/>.</returns>
    ProducesSyntax Produces(ProducesModel produces) =>
        new(naming.ToDeclarationName(produces.EventName), null, [.. produces.Mappings.Select(Mapping)], SourceLocation.Start);

    /// <summary>
    /// Builds a command a reaction executes.
    /// </summary>
    /// <param name="invocation">The invocation.</param>
    /// <returns>The <see cref="InvokesSyntax"/>.</returns>
    InvokesSyntax Invokes(InvocationModel invocation) =>
        new(naming.ToDeclarationName(invocation.CommandName), [.. invocation.Mappings.Select(Mapping)], SourceLocation.Start);

    /// <summary>
    /// Builds one mapping.
    /// </summary>
    /// <param name="mapping">The mapping.</param>
    /// <returns>The <see cref="PropertyMappingSyntax"/>.</returns>
    PropertyMappingSyntax Mapping(PropertyMappingModel mapping) =>
        new(naming.ToPropertyName(mapping.Property), _sources.Convert(mapping.Source), SourceLocation.Start);
}
