// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents an actor declared for a module collection.
/// </summary>
/// <param name="Id">The identity of the actor.</param>
/// <param name="Name">The name of the actor.</param>
/// <param name="ActorType">The kind of actor it is.</param>
/// <param name="Description">The description of the actor.</param>
/// <remarks>
/// A generated document never holds one. The personas a Screenplay document declares are reported as a
/// warning rather than drawn, since the board states an actor against a collection and Screenplay does not.
/// </remarks>
public record Actor(string Id, string Name, int ActorType, string Description);
