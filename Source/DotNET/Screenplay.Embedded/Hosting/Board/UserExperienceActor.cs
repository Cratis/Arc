// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents an actor with a prototyped user experience on a slice.
/// </summary>
/// <param name="Id">The identity of the actor.</param>
/// <param name="Type">The kind of actor it is.</param>
/// <remarks>
/// A generated document never holds one: the screens a Screenplay document declares are reported as a
/// warning rather than drawn, since the board holds a prototype rather than a screen declaration.
/// </remarks>
public record UserExperienceActor(string Id, int Type);
