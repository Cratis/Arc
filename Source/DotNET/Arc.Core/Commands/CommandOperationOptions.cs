// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Configures in-process command operation recovery.
/// </summary>
public class CommandOperationOptions
{
    /// <summary>
    /// Gets or sets the cooperative budget shared by all compensators. Defaults to thirty seconds.
    /// User code ignoring cancellation is awaited; its services are never disposed underneath it.
    /// </summary>
    public TimeSpan CompensationTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
