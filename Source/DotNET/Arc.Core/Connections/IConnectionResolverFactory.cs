// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// Defines a factory for the <see cref="ICachingConnectionResolver"/> described by <see cref="ConnectionResolutionOptions"/>.
/// </summary>
public interface IConnectionResolverFactory
{
    /// <summary>
    /// Create a resolver from options.
    /// </summary>
    /// <param name="options">The <see cref="ConnectionResolutionOptions"/> describing the resolvers.</param>
    /// <returns>An <see cref="ICachingConnectionResolver"/> that resolves through the configured slots.</returns>
    ICachingConnectionResolver Create(ConnectionResolutionOptions options);
}
