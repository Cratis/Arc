// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Supplies startup admission metadata when Chronicle discovery is available.
/// </summary>
internal interface IStartupDecisionReads
{
    /// <summary>
    /// Gets the event store's admission reader without a namespace-specific read.
    /// </summary>
    /// <param name="cancellationToken">The host shutdown token.</param>
    /// <returns>The reader, or null when discovery or the connection is unavailable.</returns>
    Task<IDecisionReads?> Get(CancellationToken cancellationToken);
}
