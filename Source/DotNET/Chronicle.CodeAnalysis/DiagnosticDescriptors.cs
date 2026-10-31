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
        messageFormat: "Reactor '{0}' reaches the default event log through '{1}'. {2}.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Reactors observe events and produce side effects; they must not append to the default event log directly, whether by injecting IEventLog or by appending through an injected IEventStore (its EventLog property or GetEventSequence(EventSequenceId.Log)). Both write to the sequence the handler's return type already targets, so return the events instead — a single event, an IEnumerable<object>, or EventForEventSourceId wrappers for another event source. To close a stream, return CompleteStream from the handler instead of calling CompleteStream on the event log. To trigger work in another slice, inject ICommandPipeline and execute a command. Two shapes a returned event cannot express are not reported: routing to a different sequence, such as GetEventSequence(EventSequenceId.Outbox), and appending to an event store other than the one the reactor was handed, such as one obtained from IChronicleClient.GetEventStore.");

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
        messageFormat: "Command '{0}' injects IEventLog into '{1}' through parameter '{2}'. {3}.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A command expresses appends by returning events from Handle. Under On, immediate appends are refused even in an Unprotected method body; return the events or use IEventLog.Transactional. To close a stream, return CompleteStream.",
        helpLinkUri: "https://www.cratis.io/arc/backend/csharp/chronicle/code-analysis/arcchr0007/");

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

    /// <summary>
    /// ARCCHR0010: Raw Guid response does not set the event source id.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0010_RawGuidResponseDoesNotSetEventSourceId = new(
        id: "ARCCHR0010",
        title: "Raw Guid response does not set the event source id",
        messageFormat: "Keyless command '{0}' returns a raw Guid beside event '{1}'. The Guid is an ordinary response, not event-source metadata. If this Guid is intended to identify the event source, return EventSourceId<Guid> (prefer a derived domain identity), or declare the command's event source id.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A raw Guid in a command response tuple is an ordinary client response, not event-source metadata. Without an explicit target, Chronicle generates a fallback event source id. If this Guid is intended to identify the event source, return EventSourceId<Guid> or a derived domain identity. An intentional ordinary Guid response with a generated event source id remains valid. This heuristic examines known tuple, Task, ValueTask, Result, OneOf, and typed event collection signatures, not erased object payloads or arbitrary method bodies.");

    /// <summary>
    /// ARCCHR0011: A legacy read in an event-producing decision is not guarded.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0011_UnprotectedDecisionRead = new(
        id: "ARCCHR0011",
        title: "Plain read model in event-producing command is unprotected",
        messageFormat: "Command '{0}' reads Chronicle model '{1}' without protecting its decision. {2}. To decide over one event stream, read it with IStreamReads and append with StreamDecision.Append (see the stream decisions documentation).",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Under Off, plain Chronicle state dependencies are advisory rather than guarded. Enable On and take the model as a parameter, preferably in a CommandValidator constructor for state-dependent rules. Explicit Unprotected declarations acknowledge advisory reads. Measure and On are not reported by this Off-only rule.",
        helpLinkUri: "https://www.cratis.io/arc/backend/csharp/chronicle/code-analysis/arcchr0011/");

    /// <summary>
    /// ARCCHR0012: An immediate append cannot be covered by the command's decision guard.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0012_ImmediateAppendAfterDecisionRead = new(
        id: "ARCCHR0012",
        title: "Immediate append bypasses protected decision",
        messageFormat: "Command '{0}' immediately appends through IEventLog while its decision is protected. Return the event or use IEventLog.Transactional; an immediate append cannot be rolled back after a decision conflict. On refuses immediate appends, including in Unprotected method bodies. For a stream decision, return StreamDecision.Append from Handle.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Immediate Chronicle appends bypass the owning command's guarded commit. This rule is Info for explicit protected decisions and Warning under On. Transactional appends are excluded; method-level Unprotected permits advisory reads, not immediate appends.",
        helpLinkUri: "https://www.cratis.io/arc/backend/csharp/chronicle/code-analysis/arcchr0012/");

    /// <summary>
    /// ARCCHR0013: A legacy event source type attribute spells out a declared event source definition.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0013_UseEventSourceDefinition = new(
        id: "ARCCHR0013",
        title: "Use the event source definition instead of the event source type attribute",
        messageFormat: "'{0}' names event source '{1}' with a string, and the compilation declares that event source. Declare it with {2} so the source, its stream and its concurrency dimensions come from the definition.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "A string naming an event source or stream type can drift from the definition. The code fix uses the uniquely matching definition in this compilation, retaining a declared stream. Unmatched names are left alone.",
        helpLinkUri: "https://www.cratis.io/arc/backend/csharp/chronicle/code-analysis/arcchr0013/");

    /// <summary>
    /// ARCCHR0014: A reactor or reducer is filtered to a stream its event source definition does not declare.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0014_ObservedEventStreamNotDeclared = new(
        id: "ARCCHR0014",
        title: "Observed event stream is not declared by the event source definition",
        messageFormat: "'{0}' observes stream '{1}' of event source definition '{2}', which does not declare that stream. Declare it with [EventStream(\"{1}\")] on '{2}' or observe a stream it declares.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "[FromEventSource<TSource>(stream)] filters a reactor or reducer to a stream the definition declares with [EventStream]. A stream the definition does not declare cannot match, so the observer would never see an event. Only the definition's own attributes are read, so a definition in a referenced assembly is checked exactly like one in the compilation.");

    /// <summary>
    /// ARCCHR0015: A command handler declares a nullable event return type.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0015_NullableCommandEventReturn = new(
        id: "ARCCHR0015",
        title: "Command handler must not return a nullable event",
        messageFormat: "Command '{0}' declares nullable event '{1}' in its Handle return type. Return Result<TEvent, ValidationResult> with ValidationResult.Error(...) instead, or reject in a validator or Provide().",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A direct or awaited null event result currently succeeds without appending anything. A null event branch inside Result or OneOf instead produces a NullReferenceException failure. Neither gives the caller a rejection reason. Declare a non-nullable event success branch and reject explicitly with ValidationResult.Error, a validator, or Provide. This rule checks direct events, Task/ValueTask results, and Result/OneOf event branches. Optional ICommandOperation results and non-event responses are not reported.");

    /// <summary>
    /// ARCCHR0016: A stream-metadata attribute asks for the concurrency scope while Handle returns exact scopes.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0016_ConcurrencyAttributeIgnoredByExactScopes = new(
        id: "ARCCHR0016",
        title: "Concurrency flag is ignored when the handler returns exact concurrency scopes",
        messageFormat: "Command '{0}' sets concurrency: true on [{1}], but its Handle method returns EventsWithConcurrencyScopes, whose exact scopes replace the attribute-derived scope. Remove concurrency: true.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "[EventSourceType], [EventStreamType] and [EventStreamId] take a concurrency flag that makes the attribute part of the concurrency scope Arc derives for the command's events. When Handle returns EventsWithConcurrencyScopes (alone, in a Task or ValueTask, or in a tuple), for example from StreamDecision.Append, the returned scopes are used as they are and nothing is derived from the attributes, so the flag has no effect. Event source definitions ([EventSource<T>] with concurrency dimensions) are not reported: a definition is a shared policy the command cannot remove, and an exact scope intentionally replaces it.");

    /// <summary>
    /// ARCCHR0017: An [EventStreamId] template is invalid.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0017_InvalidEventStreamIdTemplate = new(
        id: "ARCCHR0017",
        title: "[EventStreamId] template is invalid",
        messageFormat: "Command '{0}' has an invalid [EventStreamId] template '{1}': {2}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A value with {Name} placeholders is resolved from the command at runtime. Each placeholder must name a public instance property of the command whose type converts to a string: a string, a primitive, decimal, Guid, enum, DateOnly, DateTime, DateTimeOffset, TimeOnly, TimeSpan, a ConceptAs<T> over one of these, or an EventSourceId. Braces must balance; write {{ and }} for literal braces. A value without placeholders is a constant stream id and is not checked.");

    /// <summary>
    /// ARCCHR0018: An [EventStreamId] template on a type that is not an Arc command.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0018_EventStreamIdTemplateOnlyResolvedForCommands = new(
        id: "ARCCHR0018",
        title: "Event stream id template is only resolved for commands",
        messageFormat: "Reactor '{0}' has [EventStreamId] with the template '{1}', but only Arc commands resolve {{Property}} placeholders. A reactor uses the value as a literal stream id.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Chronicle reactors read [EventStreamId] as a constant, so the braces in a template stay in the stream id of the side-effect events the reactor appends. Only commands are resolved against their properties. Use a constant stream id here, or return the events with an explicit route.");

    /// <summary>
    /// ARCCHR0019: A conventional query resembles a read-model fold.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0019_ReadModelFoldMayBeQuery = new(
        id: "ARCCHR0019",
        title: "Read-model fold may be exposed as a query",
        messageFormat: "Read model '{0}' conventionally exposes '{1}', which resembles a fold. If it is a helper, set RequireQueryAttribute = true and mark only intended endpoints with [Query]; preview the endpoint changes before applying.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "This heuristic recognizes Create(event[, context]) and Reduce(event, nullable current[, context]) returning the read model or Task of it. It does not prove event registration or change discovery. Opt into explicit query selection to keep folds out of the endpoint surface. Suppress with a justification when this is an intentional query.");

    /// <summary>
    /// ARCCHR0020: A wrapper names a stream its event source definition does not declare.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0020_WrapperEventStreamNotDeclared = new(
        id: "ARCCHR0020",
        title: "Wrapper event stream is not declared",
        messageFormat: "Wrapper stream '{0}' is not declared by event source definition '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Literal wrapper routing must name a stream declared by its known event source definition. Alternative declared streams remain supported.");

    /// <summary>
    /// ARCCHR0021: Legacy wrapper strings name a declared event source stream but drop its definition.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0021_UseEventSourceDefinitionRouting = new(
        id: "ARCCHR0021",
        title: "Use event source definition routing",
        messageFormat: "Legacy wrapper routing to '{0}.{1}' {2}; use EventSource = typeof({3}) and EventStream = \"{1}\"",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Legacy strings keep their routing behavior but omit the event source definition and its concurrency dimensions. Use typed routing to retain them.");

    /// <summary>
    /// ARCCHR0022: Command-key aggregate accompanies events for a generated source.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0022_AggregateOnGeneratedTarget = new(
        id: "ARCCHR0022",
        title: "Command-key aggregate on a generated append target",
        messageFormat: "Aggregate parameter '{0}' resolves using the command's event source id, but returned events target generated parameter '{1}'. Resolve the aggregate explicitly for the intended source, or use the command's source for the events.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Automatically injected aggregates use the command's event source id, not a generated append target. Commands with Provide or custom id providers are excluded because their aggregate resolution cannot be proven statically.");

    /// <summary>
    /// ARCCHR0027: An OnRefused callback returns a statically invalid production.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0027_InvalidRefusalBranchReturn = new(
        id: "ARCCHR0027",
        title: "Refusal branch must return registered events or acknowledge",
        messageFormat: "This OnRefused callback returns an invalid refusal production. Return registered events, EventForEventSourceId envelopes containing events, or a nonempty collection of those; return null or use Acknowledge to acknowledge without events. Commands, operations, completions and non-event objects are not refusal productions.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A refusal branch is validated in full before any branch events are appended. Statically visible invalid results fail the reactor delivery at runtime. This rule checks visible lambdas, anonymous methods and source method-group bodies, including conditional returns and literal batches; opaque helpers remain runtime-validated.");

    /// <summary>
    /// ARCCHR0023: A read-model parameter's intended lookup key is not proven.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0023_ReadModelLookupNotProven = new(
        id: "ARCCHR0023",
        title: "Read-model lookup key is not proven",
        messageFormat: "Read-model parameter '{0}' on command '{1}' has no proven lookup key: {2}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "On selects a unique property of the model key type and refuses ambiguity. Off and Measure keep the command key unless LookUpBy redirects it. Explicit LookUpBy is not reported; repeated inferred keys are advisory. Runtime dataflow is not proven.",
        helpLinkUri: "https://www.cratis.io/arc/backend/csharp/chronicle/code-analysis/arcchr0023/");

    /// <summary>
    /// ARCCHR0025: A keyless event-producing command depends on Chronicle state.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0025_KeylessCommandRead = new(
        id: "ARCCHR0025",
        title: "Keyless command reads Chronicle state",
        messageFormat: "Command '{0}' has no event source key; {1}. Add a key property or implement ICanProvideEventSourceId.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Automatically injected Chronicle read models in a keyless command are looked up using a newly generated id. IReadModels and IEventStore dependencies are advisory because their explicit lookup keys are not proven.");

    /// <summary>
    /// ARCCHR0026: A MongoDB collection's model is backed by Chronicle.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0026_ChronicleModelThroughMongoCollection = new(
        id: "ARCCHR0026",
        title: "Chronicle model read through a MongoDB collection",
        messageFormat: "MongoDB collection parameter '{0}' contains Chronicle-backed model '{1}'; a read through this collection is not proven protected. Inject the model for a keyed read, or express an invariant as a constraint.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "IMongoCollection of a Chronicle-backed model is Info outside On and Warning under On, where runtime parameter admission refuses it. This recognizes model-bound metadata and source-declared backing without inspecting MongoDB queries.",
        helpLinkUri: "https://www.cratis.io/arc/backend/csharp/chronicle/code-analysis/arcchr0026/");

    /// <summary>
    /// ARCCHR0029: Conflicting protected and advisory command declarations.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0029_ProtectedAndUnprotected = new(
        id: "ARCCHR0029",
        title: "Conflicting read protection",
        messageFormat: "Command '{0}' declares both ProtectedDecision and Unprotected. Remove one of these declarations.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Protected decisions cannot also be declared advisory on the same command, including inherited declarations.",
        helpLinkUri: "https://www.cratis.io/arc/backend/csharp/chronicle/code-analysis/arcchr0029/");

    /// <summary>
    /// ARCCHR0029: Explicit read protection conflicts with a command opt-out.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0029_ReadProtectionAndUnprotected = new(
        id: "ARCCHR0029",
        title: "Conflicting read protection",
        messageFormat: "Command '{0}' declares both ReadProtection({1}) and Unprotected. Remove one of these declarations.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "An explicit command-level On or Measure declaration cannot be combined with a command-level advisory opt-out. An assembly rollout may be overridden by Unprotected.",
        helpLinkUri: "https://www.cratis.io/arc/backend/csharp/chronicle/code-analysis/arcchr0029/");

    /// <summary>
    /// ARCCHR0031: Lookup refers to a missing command property.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0031_MissingLookupProperty = new(
        id: "ARCCHR0031",
        title: "Invalid read-model lookup",
        messageFormat: "LookUpBy on parameter '{0}' names missing public instance property '{1}' on command '{2}'. Check the property name (did you mean '{3}') and use nameof for the property.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "LookUpBy must name an existing public instance command property. A suggested property can be converted to nameof by the code fix.",
        helpLinkUri: "https://www.cratis.io/arc/backend/csharp/chronicle/code-analysis/arcchr0031/");

    /// <summary>
    /// ARCCHR0031: Lookup is declared on a parameter proven not to be a read model.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0031_LookupOnNonReadModel = new(
        id: "ARCCHR0031",
        title: "Invalid read-model lookup",
        messageFormat: "LookUpBy on parameter '{0}' requires a read model, but '{1}' is not a read-model type. Remove LookUpBy or inject a read model.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Only provably non-model types are reported; backing declared in another project need not be visible to the analyzer.",
        helpLinkUri: "https://www.cratis.io/arc/backend/csharp/chronicle/code-analysis/arcchr0031/");

    /// <summary>
    /// ARCCHR0031: Lookup property and model key types differ.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0031_LookupKeyTypeMismatch = new(
        id: "ARCCHR0031",
        title: "Invalid read-model lookup",
        messageFormat: "LookUpBy on parameter '{0}' selects property '{1}' of type '{2}', but read model '{3}' has key type '{4}'. Select a property of the model key type.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "When the model key is statically known, the explicit lookup property must have the same type.",
        helpLinkUri: "https://www.cratis.io/arc/backend/csharp/chronicle/code-analysis/arcchr0031/");

    /// <summary>
    /// ARCCHR0032: A declaration is inert or redundant.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0032_InertReadProtection = new(
        id: "ARCCHR0032",
        title: "Inert or redundant read-protection declaration",
        messageFormat: "Declaration on '{0}' is inert or redundant: {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "ReadProtection on a sealed non-command type and Unprotected on a provably non-model parameter have no effect. Off is redundant only when the assembly is already Off.",
        helpLinkUri: "https://www.cratis.io/arc/backend/csharp/chronicle/code-analysis/arcchr0032/");

    /// <summary>
    /// ARCCHR0024: A statically known model shape cannot be protected.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0024_ReadCannotBeProtected = new(
        id: "ARCCHR0024",
        title: "Read-model shape cannot be protected",
        messageFormat: "Command '{0}' reads '{1}' ({2}) {3}, so protection is not proven. {4}. If it is only context, mark the parameter [Unprotected(\"why\")].",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "In Measure or On, statically refused projection and reducer shapes need a different protected model or an explicit advisory declaration. Unfiltered reducers without constructor dependencies are admitted. Runtime registration and delivery choices not visible in source are not inferred.",
        helpLinkUri: "https://www.cratis.io/arc/backend/csharp/chronicle/code-analysis/arcchr0024/");

    /// <summary>
    /// ARCCHR0028: An On command reaches an unguarded Chronicle API.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0028_RawReadUnderOn = new(
        id: "ARCCHR0028",
        title: "Chronicle read is not proven protected",
        messageFormat: "Command '{0}' calls '{1}' while read protection is On; this read is not proven protected. Take a read model as a parameter, use IDecisionReads.Get in Provide, or mark the method [Unprotected(\"why\")].",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Raw Chronicle reads, detached reads and explicit read allowances in application On commands bypass automatic protected parameter resolution. Advisory method bodies are excluded, but AllowReads itself remains an explicit, reviewable bypass.",
        helpLinkUri: "https://www.cratis.io/arc/backend/csharp/chronicle/code-analysis/arcchr0028/");

    /// <summary>
    /// ARCCHR0033: An aggregate and a plain read use the same command key.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0033_ReadAndAggregateOnSameSource = new(
        id: "ARCCHR0033",
        title: "Aggregate and read-model parameter share a source",
        messageFormat: "Command '{0}' takes aggregate '{1}' and read-model parameter '{2}' on key '{3}'; their combined protection is not proven. Use the aggregate's state, or read a different source explicitly.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "On refuses an aggregate and a protected plain read of the same source. Only automatic dependencies whose command key is statically the same are reported; explicit providers and advisory reads are excluded.",
        helpLinkUri: "https://www.cratis.io/arc/backend/csharp/chronicle/code-analysis/arcchr0033/");

    /// <summary>
    /// ARCCHR0034: Method-level Unprotected does not cover injected parameters.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0034_UnprotectedBodyHasReadParameters = new(
        id: "ARCCHR0034",
        title: "Unprotected method does not opt out read-model parameters",
        messageFormat: "Method '{0}' is Unprotected, but read-model parameter '{1}' is resolved before its body. Mark the parameter or model [Unprotected(\"why\")] if that read is advisory.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "A method-level advisory declaration covers only the method body. It never changes the protection of read-model parameters resolved before invocation.",
        helpLinkUri: "https://www.cratis.io/arc/backend/csharp/chronicle/code-analysis/arcchr0034/");

    /// <summary>
    /// ARCCHR0035: Aggregate commit completes protection too early.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0035_EarlyProtectedCommit = new(
        id: "ARCCHR0035",
        title: "Protected aggregate is committed before the outer decision ends",
        messageFormat: "Command '{0}' calls aggregate.Commit() {1}; subsequent protection is not proven. Read before committing and return Commit() last in the outer command.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "In On, aggregate Commit completes the unit. A nested command must leave completion to its owner; reads or appends after Commit in the same straight-line block are refused. Opaque helpers and arbitrary control flow are not inferred.",
        helpLinkUri: "https://www.cratis.io/arc/backend/csharp/chronicle/code-analysis/arcchr0035/",
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    /// <summary>
    /// ARCCHR0036: Exact scopes are not proven to cover a read of the same source (0027 is occupied).
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0036_ExactScopeCoverageNotProven = new(
        id: "ARCCHR0036",
        title: "Exact scope coverage of a protected read is not proven",
        messageFormat: "Command '{0}' returns exact scopes or a stream decision beside read-model parameter '{1}' on key '{2}'; coverage of that read is not proven. Widen the exact scope to cover the read, or remove the exact scope.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Measure and On commands returning exact concurrency scopes must cover the source, types, dimensions and read boundary of every protected read on that source. This signature heuristic reports only automatic reads on the command source; explicit providers and advisory parameters are excluded.",
        helpLinkUri: "https://www.cratis.io/arc/backend/csharp/chronicle/code-analysis/arcchr0036/");

    /// <summary>
    /// ARCCHR0037: A stream model does not belong to the command's route.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0037_ReadModelStreamNotOnCommandRoute = new(
        id: "ARCCHR0037",
        title: "Read model stream is not on the command route",
        messageFormat: "Command '{0}' reads '{1}' ({2}), scoped to {3}/{4}, but {5}. Route the command to that stream with an explicit EventStreamId, or use IDecisionReads in Provide for another stream.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Per-stream model parameters, including validator constructor parameters, bind only to the command's own source and stream. LookUpBy cannot redirect a stream model. This applies in Off, Measure and On.",
        helpLinkUri: "https://www.cratis.io/arc/backend/csharp/chronicle/code-analysis/arcchr0037/");

    /// <summary>
    /// ARCCHR0038: Combining reads or declared routing can widen a stream guard.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0038_StreamReadGuardWidens = new(
        id: "ARCCHR0038",
        title: "Stream read guard widens",
        messageFormat: "Command '{0}' reads per-stream model '{1}', but {2}; the guard is not proven narrow and can widen to the whole source. Use only stream state when other periods must not conflict.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Under Measure or On, a whole-source read on the command source, a read on another stream, or effective declared Concurrency without both stream dimensions can widen a stream read guard. Widening causes extra conflicts, not missed changes.",
        helpLinkUri: "https://www.cratis.io/arc/backend/csharp/chronicle/code-analysis/arcchr0038/");

    /// <summary>
    /// ARCCHR0039: A handler exposes decision acquisition instead of plain state.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0039_NonIdiomaticDecisionParameter = new(
        id: "ARCCHR0039",
        title: "Prefer plain read-model parameters in Handle",
        messageFormat: "Command '{0}' takes '{1}' in Handle. Take the read model as a plain parameter, use LookUpBy for a command property key, or a per-stream model for the command route. Only keys no parameter can express belong in Provide(IDecisionReads), returning plain values to Handle.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Decision reader and token parameters remain supported, but application handlers should decide from plain state. This signature advice applies in Off, Measure and On and does not change runtime guard behavior.",
        helpLinkUri: "https://www.cratis.io/arc/backend/csharp/chronicle/code-analysis/arcchr0039/");

    const string Category = "Arc.Chronicle";
}
