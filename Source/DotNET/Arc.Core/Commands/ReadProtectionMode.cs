// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// The declared read-protection rollout mode.
/// </summary>
public enum ReadProtectionMode
{
    /// <summary>
    /// Preserve existing command read behavior.
    /// </summary>
    Off = 0,

    /// <summary>
    /// Measure reads without enforcing protection.
    /// </summary>
    Measure = 1,

    /// <summary>
    /// Declare that command reads require protection.
    /// </summary>
    On = 2
}
