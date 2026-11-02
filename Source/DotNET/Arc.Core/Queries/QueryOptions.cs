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

    /// <summary>
    /// Gets or sets the origins that may open observable query WebSocket connections from a browser, in addition to the
    /// origin the application itself is served from. Entries are origins such as <c>https://app.example.com</c>; a single
    /// <c>*</c> entry accepts any origin. Connections without an <c>Origin</c> header, as sent by non-browser clients, are
    /// always accepted. While the list is empty, only the application's own origin is accepted.
    /// </summary>
    public IList<string> AllowedWebSocketOrigins { get; set; } = [];
}
