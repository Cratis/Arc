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
public record ProjectionModel(
    string Identifier,
    string ReadModel,
    string EventSequenceId,
    ProjectionAutoMapMode AutoMap,
    bool SubscribesToAllEvents,
    ProjectionScopeModel Scope);
