// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Contexts;

/// <summary>
/// Exception that gets thrown when a context value could not be resolved, such as when a store is unavailable or a provider fails.
/// </summary>
/// <remarks>
/// It is a retryable error and is never reported as a denial, so an outage is not shown as "not available".
/// </remarks>
/// <param name="name">The name of the value.</param>
/// <param name="innerException">The cause.</param>
public class ContextValueUnavailableException(string name, Exception? innerException = null)
    : Exception($"The context value '{name}' could not be resolved. Retry the request.", innerException)
{
    /// <summary>
    /// Gets the name of the value.
    /// </summary>
    public string Name { get; } = name;
}
