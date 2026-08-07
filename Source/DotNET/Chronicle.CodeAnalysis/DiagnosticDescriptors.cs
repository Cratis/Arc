// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Diagnostic descriptors for Arc Chronicle analyzers.
/// </summary>
static class DiagnosticDescriptors
{
    /// <summary>
    /// ARCCHR0001: Incorrect AggregateRoot event handler signature.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0001_IncorrectAggregateRootEventHandlerSignature = new(
        id: "ARCCHR0001",
        title: "Incorrect AggregateRoot event handler signature",
        messageFormat: "Event handler method '{0}' on AggregateRoot '{1}' must have one of these signatures: void On(TEvent), Task On(TEvent), void On(TEvent, EventContext), or Task On(TEvent, EventContext). Found: {2}.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Event handler methods (typically named 'On') on AggregateRoot types must accept an event parameter and optionally an EventContext parameter, and return void or Task.");

    /// <summary>
    /// ARCCHR0002: Command has ambiguous event source id with multiple candidate properties.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0002_AmbiguousCommandEventSourceId = new(
        id: "ARCCHR0002",
        title: "Command has ambiguous event source id and should implement ICanProvideEventSourceId",
        messageFormat: "Command '{0}' has multiple event source id candidate properties ({1}) but does not implement ICanProvideEventSourceId. Implement ICanProvideEventSourceId to make the default event source id explicit.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When a command exposes more than one property that can resolve to an EventSourceId (an EventSourceId, an EventSourceId<T>, a type with an implicit conversion to EventSourceId, or a [Key]-marked property), the framework resolves the event source id from the first matching property, which is ambiguous. Implement ICanProvideEventSourceId to declare which value to use. This is not required when the command's Handle method returns only EventForEventSourceId events, since each such event carries its own event source id.");

    /// <summary>
    /// ARCCHR0003: Reactor must not reach the default event log.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0003_ReactorMustNotReachEventLog = new(
        id: "ARCCHR0003",
        title: "Reactor must not reach the default event log",
        messageFormat: "Reactor '{0}' reaches the default event log through '{1}'. Return the events from the handler method — a single event, an IEnumerable<object>, or EventForEventSourceId wrappers — instead of appending directly.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Reactors observe events and produce side effects; they must not append to the default event log directly, whether by injecting IEventLog or by appending through an injected IEventStore (its EventLog property or GetEventSequence(EventSequenceId.Log)). Both write to the sequence the handler's return type already targets, so return the events instead — a single event, an IEnumerable<object>, or EventForEventSourceId wrappers for another event source. To trigger work in another slice, inject ICommandPipeline and execute a command. Routing to a different sequence, such as GetEventSequence(EventSequenceId.Outbox), is not reported: a returned event cannot target a sequence other than the default log.");

    /// <summary>
    /// ARCCHR0004: [EventType] should not specify an explicit id.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0004_EventTypeShouldNotSpecifyId = new(
        id: "ARCCHR0004",
        title: "[EventType] should not specify an explicit id",
        messageFormat: "Event type '{0}' specifies an explicit id on [EventType]. Remove the id argument — the type name is used as the identifier automatically.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The [EventType] attribute derives its identifier from the type name by convention, so an explicit id should not be passed. Use a bare [EventType]. The generation argument is still allowed for event evolution.");

    /// <summary>
    /// ARCCHR0005: Chronicle artifacts are present but Chronicle is not wired up.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0005_ChronicleArtifactsWithoutWithChronicle = new(
        id: "ARCCHR0005",
        title: "Chronicle is used but not wired up",
        messageFormat: "This project sets up Arc with AddCratisArc but never calls WithChronicle() or AddCratis(), yet it uses Chronicle features (for example '{0}'). Call WithChronicle() on the Arc builder, or use AddCratis(), otherwise appending or reading events fails at runtime.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "AddCratisArc on its own wires Arc with no event store, and running Arc without Chronicle is a supported, valid setup (backed by MongoDB or EF Core). This rule only fires when the project actually uses Chronicle — an aggregate root, reactor, reducer, fluent or model-bound projection (IProjectionFor or [FromEvent]/[SetFrom]/[SetValue]/... attributes), [EventType] event, or a type that injects a Chronicle service such as IEventLog or IEventStore. In that case the event store must be added with WithChronicle() on the Arc builder, or by using the all-in-one AddCratis(); without it, any command, query, reactor, or reducer that touches Chronicle fails to resolve at runtime. This analyzer only reports when the setup call and the Chronicle usage live in the same project, so it never fires when setup is wired up in a separate host project.",
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    /// <summary>
    /// ARCCHR0006: Reactor handler invoking ICommandPipeline.Execute must be marked with [OnceOnly].
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0006_ReactorCommandPipelineExecuteMustBeOnceOnly = new(
        id: "ARCCHR0006",
        title: "Reactor handler invoking ICommandPipeline.Execute must be marked with [OnceOnly]",
        messageFormat: "Reactor handler '{0}' invokes ICommandPipeline.Execute but is not marked [OnceOnly]; replay will re-execute the command. Mark the method [OnceOnly].",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A reactor handler that calls ICommandPipeline.Execute produces a side effect. During replay operations (redaction, revision, observer rewind), the handler runs again and re-executes the command, duplicating the side effect. Mark the method with [OnceOnly] so it is skipped during replays.");

    /// <summary>
    /// ARCCHR0007: Command Handle method must not inject IEventLog.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0007_CommandHandleMustNotInjectEventLog = new(
        id: "ARCCHR0007",
        title: "Command handler must not inject IEventLog",
        messageFormat: "Command '{0}' injects IEventLog into '{1}' through parameter '{2}'. Express every append through the handler return type, not IEventLog.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A command expresses appends by returning events from its Handle method (a single event, a tuple of event and result, a Result, or a collection). Injecting IEventLog into the handler bypasses Arc's append pipeline and its correlation and ordering guarantees. Return the events instead of appending through IEventLog directly.");

    /// <summary>
    /// ARCCHR0008: Command key marked with the data annotations Key attribute.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0008_CommandKeyMarkedWithDataAnnotationsKey = new(
        id: "ARCCHR0008",
        title: "Command key marked with the data annotations Key attribute",
        messageFormat: "Command '{0}' marks '{1}' with System.ComponentModel.DataAnnotations.KeyAttribute. Chronicle resolves keys from Cratis.Chronicle.Keys.KeyAttribute, so it will resolve a new event source id for every '{0}' and every read model keyed by it will resolve to nothing. Use Cratis.Chronicle.Keys.KeyAttribute instead.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Two attributes are spelled [Key]. Chronicle resolves a command's event source id from Cratis.Chronicle.Keys.KeyAttribute; Arc reads System.ComponentModel.DataAnnotations.KeyAttribute, but only in an application that has no Chronicle. Marking the data annotations one in an application that uses Chronicle compiles and reads correctly while doing nothing: Chronicle finds no key property, invents a fresh event source id, and every read model keyed by the command resolves to nothing.");

    const string Category = "Arc.Chronicle";
}
