// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// Defines the application's connection resolvers, shared by every storage integration.
/// </summary>
/// <remarks>
/// The application-wide resolution is configured on <see cref="ArcOptions.ConnectionResolution"/>. A storage integration
/// passes its own <see cref="ConnectionResolutionOptions"/>; when those are configured they take precedence, otherwise
/// the application-wide resolver is used. Every integration therefore resolves through the same resolvers, the same
/// HTTP handling and the same cache.
/// </remarks>
public interface IConnectionResolvers
{
    /// <summary>
    /// Gets the resolver for a storage integration.
    /// </summary>
    /// <param name="options">The integration's own <see cref="ConnectionResolutionOptions"/>.</param>
    /// <returns>The <see cref="ICachingConnectionResolver"/>, or <see langword="null"/> when neither the integration nor
    /// the application resolves connections, in which case the configured connection is used as it is.</returns>
    ICachingConnectionResolver? For(ConnectionResolutionOptions options);
}
