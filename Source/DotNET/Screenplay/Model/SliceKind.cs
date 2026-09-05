// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents the Event Modeling kind of a slice.
/// </summary>
public enum SliceKind
{
    /// <summary>
    /// The command to events strip - it changes the system.
    /// </summary>
    StateChange = 0,

    /// <summary>
    /// The events to read model to user interface strip - it reads the system.
    /// </summary>
    StateView = 1,

    /// <summary>
    /// The event to side effect strip - it reacts to what happened.
    /// </summary>
    Automation = 2,

    /// <summary>
    /// The adaptation strip - it turns one part of the model, or an external system, into events.
    /// </summary>
    Translate = 3
}
