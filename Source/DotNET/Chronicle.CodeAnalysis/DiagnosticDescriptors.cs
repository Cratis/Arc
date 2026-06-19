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
    /// ARCCHR0003: Reactor must not inject IEventLog.
    /// </summary>
    public static readonly DiagnosticDescriptor ARCCHR0003_ReactorMustNotInjectEventLog = new(
        id: "ARCCHR0003",
        title: "Reactor must not inject IEventLog",
        messageFormat: "Reactor '{0}' injects IEventLog through parameter '{1}'. Return events from the handler method (Task<TEvent>, Task<ReactorSideEffect>, or a collection) instead of appending through IEventLog directly.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Reactors observe events and produce side effects; they must not append to the event log directly. To produce new events, return them from the handler method as Task<TEvent>, Task<ReactorSideEffect>, or a collection thereof. To trigger work in another slice, inject ICommandPipeline and execute a command. Injecting IEventLog couples the reactor to the event log and bypasses the side-effect pipeline.");

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

    const string Category = "Arc.Chronicle";
}
