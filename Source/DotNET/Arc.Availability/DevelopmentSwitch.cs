// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Defines what a developer switched for a part.
/// </summary>
public enum DevelopmentSwitch
{
    /// <summary>
    /// Nothing is switched; the gates decide.
    /// </summary>
    None = 0,

    /// <summary>
    /// The part is available whatever its gates say.
    /// </summary>
    On = 1,

    /// <summary>
    /// The part is not available.
    /// </summary>
    Off = 2
}
