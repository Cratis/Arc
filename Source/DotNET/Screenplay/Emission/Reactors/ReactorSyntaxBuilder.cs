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
    /// Gets whether ESM v10 reaction metadata can be emitted.
    /// </summary>
    public bool MetadataEnabled { get; init; } = true;

    /// <summary>
    /// Gets whether ESM v19 refusal branches can be emitted.
    /// </summary>
    public bool RefusalsEnabled { get; init; } = true;

    /// <summary>
    /// Builds the reactor declaration.
    /// </summary>
    /// <param name="reactor">The reactor to build for.</param>
    /// <param name="namespace">The namespace of the slice the reactor lives in.</param>
    /// <returns>The <see cref="ReactionSyntax"/>, or <see langword="null"/> when it observes no events.</returns>
    public ReactionSyntax? Build(ReactorModel reactor, string @namespace)
    {
        var metadata = new ReactionMetadataAdmission(Application, MetadataEnabled);
        metadata.Report(reactor, diagnostics, reactor.MetadataLocation ?? @namespace);

        var name = naming.ToDeclarationName(reactor.Name);
        var path = naming.ToFilePath(reactor.SourceFilePath) ?? SourceFilePaths.Conventional(@namespace, name);
        var file = new FileReferenceSyntax(path, SourceLocation.Start);
        var declarative = Application is null ? null : new DeclarativeReactions(naming, Application) { MetadataEnabled = MetadataEnabled, RefusalsEnabled = RefusalsEnabled };

        var triggers = reactor.ObservedEvents
            .Select(_ => (Event: _, Name: naming.ToDeclarationName(_)))
            .Where(_ => _.Name.Length > 1)
            .DistinctBy(_ => _.Name, StringComparer.Ordinal)
            .Select(_ => BuildTrigger(reactor, _.Event, _.Name, declarative, file, @namespace))
            .ToList();

        if (triggers.Count == 0)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.ReactorWithoutEvents,
                $"The reactor '{reactor.Name}' observes no events and was left out",
                @namespace);

            return null;
        }

        return new(name, triggers, SourceLocation.Start)
        {
            From = metadata.FilterOf(reactor),
            RunsAs = MetadataEnabled && ReactionMetadataAdmission.ValidIdentity(reactor) && reactor.SystemRoles is { } roles
                ? new("system", roles, SourceLocation.Start) : null
        };
    }

    ReactionTriggerSyntax BuildTrigger(ReactorModel reactor, string @event, string name, DeclarativeReactions? declarative, FileReferenceSyntax file, string location)
    {
        var reaction = declarative?.For(reactor, @event);
        if (reactor.Reactions.FirstOrDefault(_ => _.EventName == @event && _.UnprovenBatch is not null) is { } batch)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnprovenReactionBatchConstraints,
                $"The reaction of '{reactor.Name}' to '{@event}' keeps its file reference: Chronicle appends its {batch.Produces.Count()} returned events as one batch, but a Screenplay reaction appends each fact separately, and {batch.UnprovenBatch}",
                reactor.MetadataLocation ?? location);
        }

        if (reaction is null)
        {
            ReportCodeHandling(reactor, @event, reactor.MetadataLocation ?? location);
        }

        return Trigger(name, reaction, file);
    }

    void ReportCodeHandling(ReactorModel reactor, string @event, string location)
    {
        var branches = reactor.Reactions.Where(reaction => reaction.EventName == @event).SelectMany(reaction => reaction.Invokes).Any(invocation => invocation.OnRefused.Count > 0);
        var handles = branches || reactor.RefusalHandlingHandlers.Contains(@event, StringComparer.Ordinal);
        var discards = reactor.DiscardedCommandResultHandlers.Contains(@event, StringComparer.Ordinal);
        if (handles || discards)
        {
            var behavior = handles ? "handles the outcome of" : "discards the outcome of and acknowledges refusals from";
            var reason = branches && !RefusalsEnabled
                ? "refusal branches require ESM v19 and exceed ScreenplayOptions.MaximumExecutableModelVersion"
                : "the complete invocation, selectors, callback effects or their mappings cannot be proved equivalent to declarative 'on refused' branches";
            diagnostics.Information(
                ScreenplayDiagnosticCodes.UnrepresentableReactionRefusalHandling,
                $"'{reactor.Name}' {behavior} a command it executes for '{@event}'; {reason}, so the whole trigger keeps its file reference",
                location);
        }

        if (reactor.RedeliveryHandlers.Contains(@event, StringComparer.Ordinal))
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.UnrepresentableReactionRedelivery,
                $"'{reactor.Name}' uses ReactorDelivery receipts for '{@event}' to track deliveries or recover from redelivery in code; 'when redelivered' is a specification action, and receipt use alone does not prove a same-delivery redelivery specification, so the trigger keeps its file reference",
                location);
        }
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
        new(naming.ToDeclarationName(produces.EventName), null, [.. produces.Mappings.Select(Mapping)], SourceLocation.Start)
        {
            For = produces.Destination is { } destination ? _sources.Convert(destination) : null
        };

    /// <summary>
    /// Builds a command a reaction executes.
    /// </summary>
    /// <param name="invocation">The invocation.</param>
    /// <returns>The <see cref="InvokesSyntax"/>.</returns>
    InvokesSyntax Invokes(InvocationModel invocation) =>
        new(naming.ToDeclarationName(invocation.CommandName), [.. invocation.Mappings.Select(Mapping)], SourceLocation.Start)
        {
            OnRefused = invocation.OnRefused.Select(branch => new InvocationRefusalSyntax(
                branch.Selector, branch.Constraint, branch.Produces.Count == 0, branch.Produces.Select(Produces), SourceLocation.Start)).ToList()
        };

    /// <summary>
    /// Builds one mapping.
    /// </summary>
    /// <param name="mapping">The mapping.</param>
    /// <returns>The <see cref="PropertyMappingSyntax"/>.</returns>
    PropertyMappingSyntax Mapping(PropertyMappingModel mapping) =>
        new(naming.ToPropertyName(mapping.Property), _sources.Convert(mapping.Source), SourceLocation.Start);
}
