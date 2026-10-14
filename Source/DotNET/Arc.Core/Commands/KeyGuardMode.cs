// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// The declared rollout mode of the command key guard.
/// </summary>
public enum KeyGuardMode
{
    /// <summary>
    /// Keeps existing command reads and appends unchanged.
    /// </summary>
    Off = 0,

    /// <summary>
    /// Observes potential conflicts without changing reads or appends.
    /// </summary>
    Observe = 1,

    /// <summary>
    /// Selects guarded command reads.
    /// </summary>
    On = 2
}
