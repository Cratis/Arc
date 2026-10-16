// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// Defines an <see cref="IConnectionResolver"/> that keeps resolved connections in memory.
/// </summary>
public interface ICachingConnectionResolver : IConnectionResolver
{
    /// <summary>
    /// Forget the resolved connection for a request, so the next resolution asks the resolvers again.
    /// </summary>
    /// <param name="request">The <see cref="ConnectionRequest"/> to forget.</param>
    /// <remarks>
    /// Call this when a connection fails to authenticate, so rotated credentials are picked up.
    /// </remarks>
    void Invalidate(ConnectionRequest request);
}
