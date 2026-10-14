// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Identifies a command key guard declaration without depending on its provider.
/// </summary>
public interface IKeyGuardAttribute
{
    /// <summary>
    /// Gets the declared rollout mode.
    /// </summary>
    KeyGuardMode Mode { get; }
}
