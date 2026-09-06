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
        description: "Reactors observe events and produce side effects; they must not append to the default event log directly, whether by injecting IEventLog or by appending through an injected IEventStore (its EventLog property or GetEventSequence(EventSequenceId.Log)). Both write to the sequence the handler's return type already targets, so return the events instead — a single event, an IEnumerable<object>, or EventForEventSourceId wrappers for another event source. To trigger work in another slice, inject ICommandPipeline and execute a command. Two shapes a returned event cannot express are not reported: routing to a different sequence, such as GetEventSequence(EventSequenceId.Outbox), and appending to an event store other than the one the reactor was handed, such as one obtained from IChronicleClient.GetEventStore.");

    /// <summary>
    /// ARCCHR0004: [EventType] repeats the type name as its id.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0004_EventTypeIdRepeatsTypeName = new(
        id: "ARCCHR0004",
        title: "[EventType] repeats the type name as its id",
        messageFormat: "Event type '{0}' passes its own name as the id on [EventType]. Remove the id argument — the type name is what the identifier defaults to, so it changes nothing.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "This rule only reports the redundant case: an id equal to the type's own name, or an empty string, neither of which changes what the event type resolves to. An id that differs from the type name is the documented way to rename an event record while stored events keep resolving under the old identifier — removing it there would orphan every stored event of that type, so it is left alone. A non-constant id cannot be evaluated at compile time and is also left alone. The generation argument is never reported.");

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
    /// ARCCHR0006: Reactor handler invoking ICommandPipeline.Execute does not say what replay should do.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0006_ReactorCommandPipelineExecuteNeedsReplayDecision = new(
        id: "ARCCHR0006",
        title: "Reactor handler invoking ICommandPipeline.Execute does not say what replay should do",
        messageFormat: "Reactor handler {0} invokes ICommandPipeline.Execute, and replay will execute the command again. Mark the handler [OnceOnly] to skip it on replay — that fires once per event source, not once per event, so a handler for an event that recurs on the same source would then run only for the first one — or declare a [Replay] handler for the same event type to take over during a replay.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "[OnceOnly] and [Replay] are not interchangeable. [OnceOnly] skips the handler entirely during replay, once per event source — a handler for an event that recurs on the same source runs only for the first occurrence, which is silently wrong for a handler that has to run for every one of them. [Replay] instead declares a separate handler for the same event type that takes over during a replay, which is the right shape whenever replay should do something rather than nothing. This rule considers only the methods Chronicle's dispatch would actually select for an event type, and follows a call through private helpers on the same reactor back to the handler that reaches it. When neither mechanism fits, suppress the diagnostic with a justification rather than mis-marking the handler [OnceOnly] to silence it.");

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

    /// <summary>
    /// ARCCHR0009: Command property reads as a secret and should be marked [NotAudited].
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0009_CommandSensitiveValueShouldNotBeAudited = new(
        id: "ARCCHR0009",
        title: "Command property reads as a secret and should be marked [NotAudited]",
        messageFormat: "Command '{0}' carries '{1}', whose name reads as a secret, and its value will be written to the causation of every event the command appends. Mark it [NotAudited], or [PII] if it is personal data.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A command's property values are recorded on the causation chain, which is written into the event log and stays there for as long as the events do - a secret written there cannot be taken back out by changing code. This rule reports a property whose name contains a word that reads as a secret (password, token, api key, credential, pin, cvv and the like) and which is not marked [NotAudited]. Marking the property, its positional parameter, or the command itself silences it, as does marking the value [PII], since Chronicle already withholds personal data. If the name only reads like a secret and the value is safe to record, mark it [NotAudited] anyway or rename the property - the value is written either way, so the reading is the only thing anyone reviewing the model has to go on.");

    const string Category = "Arc.Chronicle";
}
