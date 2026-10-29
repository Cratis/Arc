// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents something that observes events and causes an effect.
/// </summary>
/// <param name="Name">The name of the reactor.</param>
/// <param name="ObservedEvents">The names of the events the reactor observes.</param>
/// <param name="IsTranslating">The compatibility flag for event/command side effects or non-default sequence observation; it does not determine the slice kind.</param>
/// <param name="SourceFilePath">The path of the file implementing the reactor, if it is known.</param>
/// <param name="EventSource">The event source definition and stream the reactor is filtered to, if it declares one.</param>
public record ReactorModel(
    string Name,
    IEnumerable<string> ObservedEvents,
    bool IsTranslating,
    string? SourceFilePath,
    ObservedEventSourceModel? EventSource)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReactorModel"/> record for a reactor that observes no particular event source.
    /// </summary>
    /// <param name="name">The name of the reactor.</param>
    /// <param name="observedEvents">The names of the events the reactor observes.</param>
    /// <param name="isTranslating">The compatibility flag for event/command side effects or non-default sequence observation; it does not determine the slice kind.</param>
    /// <param name="sourceFilePath">The path of the file implementing the reactor, if it is known.</param>
    public ReactorModel(string name, IEnumerable<string> observedEvents, bool isTranslating, string? sourceFilePath)
        : this(name, observedEvents, isTranslating, sourceFilePath, null)
    {
    }

    /// <summary>
    /// Gets the handlers of the reactor whose whole effect can be stated declaratively, one per observed event.
    /// </summary>
    /// <remarks>
    /// An observed event without an entry here is handled by code, and the document points at the file for it.
    /// </remarks>
    public IEnumerable<ReactionModel> Reactions { get; init; } = [];

    /// <summary>
    /// Gets the exact roles for returned commands executed as the system; null means no system identity declaration.
    /// </summary>
    /// <remarks>
    /// An empty collection declares an authenticated system identity with no roles.
    /// </remarks>
    public IEnumerable<string>? SystemRoles { get; init; }

    /// <summary>
    /// Gets the observed events whose handlers inspect an executed command's outcome.
    /// </summary>
    internal IReadOnlyList<string> RefusalHandlingHandlers { get; init; } = [];

    /// <summary>
    /// Gets the observed events whose handlers discard an executed command's outcome.
    /// </summary>
    internal IReadOnlyList<string> DiscardedCommandResultHandlers { get; init; } = [];

    /// <summary>
    /// Gets the observed events whose handlers use delivery receipts.
    /// </summary>
    internal IReadOnlyList<string> RedeliveryHandlers { get; init; } = [];

    /// <summary>
    /// Gets genuine source definitions available to observation admission.
    /// </summary>
    internal IEnumerable<ReactorSourceDefinition> ObservationDefinitions { get; init; } = [];

    /// <summary>
    /// Gets the reason identity metadata cannot bind.
    /// </summary>
    internal string? IdentityFailure { get; init; }

    /// <summary>
    /// Gets the reason observation metadata cannot bind.
    /// </summary>
    internal string? ObservationFailure { get; init; }

    /// <summary>
    /// Gets whether returned events require unsupported append routing.
    /// </summary>
    internal bool HasUnrepresentableAppendRoute { get; init; }

    /// <summary>
    /// Gets the declaring type for located metadata refusals.
    /// </summary>
    internal string? MetadataLocation { get; init; }

    /// <summary>
    /// Deconstructs the reactor into the members it had before it could be filtered to an event source, so existing deconstruction keeps compiling.
    /// </summary>
    /// <param name="name">The name of the reactor.</param>
    /// <param name="observedEvents">The names of the events the reactor observes.</param>
    /// <param name="isTranslating">The compatibility flag for event/command side effects or non-default sequence observation; it does not determine the slice kind.</param>
    /// <param name="sourceFilePath">The path of the file implementing the reactor, if it is known.</param>
    public void Deconstruct(out string name, out IEnumerable<string> observedEvents, out bool isTranslating, out string? sourceFilePath)
    {
        name = Name;
        observedEvents = ObservedEvents;
        isTranslating = IsTranslating;
        sourceFilePath = SourceFilePath;
    }
}
