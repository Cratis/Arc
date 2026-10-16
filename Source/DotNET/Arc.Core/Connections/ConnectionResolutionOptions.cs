// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// Represents the options for resolving connections.
/// </summary>
/// <remarks>
/// A connection is resolved through two slots, each with the same shape. The result of <see cref="Connection"/> is
/// applied over the configured connection first, and the result of <see cref="Identity"/> on top of that. This makes
/// it possible to keep a configured connection string and only resolve the identity, or the other way around.
/// </remarks>
public class ConnectionResolutionOptions
{
    /// <summary>
    /// Gets or sets the resolver for where to connect — connection string, database and schema.
    /// </summary>
    public ConnectionResolverOptions Connection { get; set; } = new();

    /// <summary>
    /// Gets or sets the resolver for who to connect as.
    /// </summary>
    public ConnectionResolverOptions Identity { get; set; } = new();

    /// <summary>
    /// Gets or sets how long a resolved connection is kept in memory when the resolver does not say when it expires.
    /// </summary>
    /// <remarks>
    /// Resolved connections are only ever kept in memory. Set to <see cref="TimeSpan.Zero"/> to resolve every time.
    /// </remarks>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets a value indicating whether any slot resolves anything. When not, the configured connection is used as it is.
    /// </summary>
    public bool IsConfigured =>
        Connection.Resolver != ConnectionResolverType.Default ||
        Identity.Resolver != ConnectionResolverType.Default;
}
