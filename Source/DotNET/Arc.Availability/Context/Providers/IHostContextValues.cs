// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.Context.Providers;

/// <summary>
/// Defines the values the host supplies by name, for 'provide ... from host'.
/// </summary>
public interface IHostContextValues
{
    /// <summary>
    /// Gets a value the host supplies.
    /// </summary>
    /// <param name="name">The name the model declared.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The outcome; absent when the host does not supply it.</returns>
    ValueTask<ContextValueOutcome> Get(string name, CancellationToken cancellationToken);
}
