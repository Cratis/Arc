// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the options for observable queries.
/// </summary>
public class QueryOptions
{
    /// <summary>
    /// Gets or sets the keep-alive interval for observable query hub connections.
    /// The hub sends a keep-alive message only when no other message has been sent within this interval.
    /// Set to <see cref="TimeSpan.Zero"/> or a negative value to disable keep-alive.
    /// Defaults to 30 seconds.
    /// </summary>
    public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(30);
}
