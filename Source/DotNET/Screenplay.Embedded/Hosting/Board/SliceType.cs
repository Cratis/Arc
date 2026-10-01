// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents the kind of a slice on the board.
/// </summary>
/// <remarks>
/// The values are the ones the board reads; <see cref="Cratis.Screenplay.Syntax.SliceType"/> is translated
/// into them explicitly rather than cast, so a future divergence shows up as a compile error instead of a
/// slice drawn as the wrong kind.
/// </remarks>
public enum SliceType
{
    /// <summary>
    /// The slice changes state - it handles a command and produces events.
    /// </summary>
    StateChange = 0,

    /// <summary>
    /// The slice views state - it builds a read model and answers queries.
    /// </summary>
    StateView = 1,

    /// <summary>
    /// The slice automates - it reacts to events or a schedule.
    /// </summary>
    Automation = 2,

    /// <summary>
    /// The slice translates between the application and something outside it.
    /// </summary>
    Translator = 3
}
