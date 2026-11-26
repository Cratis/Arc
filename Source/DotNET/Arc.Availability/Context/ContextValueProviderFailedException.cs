// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.Context;

/// <summary>
/// The exception that is thrown when a provider fails: a store is unavailable, code throws or a call times out.
/// </summary>
/// <remarks>
/// This is an error to retry and log. It is never reported as a denial, so an outage is not shown as "not available".
/// </remarks>
/// <param name="name">The name of the value whose provider failed.</param>
/// <param name="inner">The failure.</param>
public class ContextValueProviderFailedException(string name, Exception inner) : Exception($"The provider of the context value '{name}' failed.", inner)
{
    /// <summary>
    /// Gets the name of the value whose provider failed.
    /// </summary>
    public string Name { get; } = name;
}
