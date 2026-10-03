// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a projection that builds a read model from events.
/// </summary>
/// <param name="Identifier">The identifier of the projection.</param>
/// <param name="ReadModel">The name of the read model the projection builds.</param>
/// <param name="EventSequenceId">The identifier of the event sequence the projection observes.</param>
/// <param name="AutoMap">How automatic property mapping applies at the root.</param>
/// <param name="SubscribesToAllEvents">Whether the projection observes every event type in the system.</param>
/// <param name="Scope">Everything the projection declares at its root.</param>
/// <param name="EventSource">The event source definition and stream the projection, when it was recovered from a reducer, is filtered to.</param>
public record ProjectionModel(
    string Identifier,
    string ReadModel,
    string EventSequenceId,
    ProjectionAutoMapMode AutoMap,
    bool SubscribesToAllEvents,
    ProjectionScopeModel Scope,
    ObservedEventSourceModel? EventSource)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionModel"/> record for a projection that is not filtered to an event source.
    /// </summary>
    /// <param name="identifier">The identifier of the projection.</param>
    /// <param name="readModel">The name of the read model the projection builds.</param>
    /// <param name="eventSequenceId">The identifier of the event sequence the projection observes.</param>
    /// <param name="autoMap">How automatic property mapping applies at the root.</param>
    /// <param name="subscribesToAllEvents">Whether the projection observes every event type in the system.</param>
    /// <param name="scope">Everything the projection declares at its root.</param>
    public ProjectionModel(
        string identifier,
        string readModel,
        string eventSequenceId,
        ProjectionAutoMapMode autoMap,
        bool subscribesToAllEvents,
        ProjectionScopeModel scope)
        : this(identifier, readModel, eventSequenceId, autoMap, subscribesToAllEvents, scope, null)
    {
    }

    /// <summary>
    /// Deconstructs the projection into the members it had before it could be filtered to an event source, so existing deconstruction keeps compiling.
    /// </summary>
    /// <param name="identifier">The identifier of the projection.</param>
    /// <param name="readModel">The name of the read model the projection builds.</param>
    /// <param name="eventSequenceId">The identifier of the event sequence the projection observes.</param>
    /// <param name="autoMap">How automatic property mapping applies at the root.</param>
    /// <param name="subscribesToAllEvents">Whether the projection observes every event type in the system.</param>
    /// <param name="scope">Everything the projection declares at its root.</param>
    public void Deconstruct(
        out string identifier,
        out string readModel,
        out string eventSequenceId,
        out ProjectionAutoMapMode autoMap,
        out bool subscribesToAllEvents,
        out ProjectionScopeModel scope)
    {
        identifier = Identifier;
        readModel = ReadModel;
        eventSequenceId = EventSequenceId;
        autoMap = AutoMap;
        subscribesToAllEvents = SubscribesToAllEvents;
        scope = Scope;
    }
}
