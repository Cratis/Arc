// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents what a reactor does when one event reaches it, stated declaratively.
/// </summary>
/// <param name="EventName">The name of the event the handler observes.</param>
/// <param name="Produces">The events the handler appends to the event source of the triggering event, in order.</param>
/// <param name="Invokes">The commands the handler hands to the command pipeline, in order.</param>
/// <remarks>
/// Only a handler whose whole effect is what it returns is held here: it unconditionally returns an event, a command,
/// or a collection of either, and every value it gives them is read from the triggering event, its occurrence time or
/// a constant. Any other handler is code, and the reactor keeps pointing at the file it is written in for that event.
/// </remarks>
public record ReactionModel(string EventName, IEnumerable<ProducesModel> Produces, IEnumerable<InvocationModel> Invokes);
