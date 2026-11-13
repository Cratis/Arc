// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Contexts;

/// <summary>
/// Exception that gets thrown when a context value declared as required is absent.
/// </summary>
/// <remarks>
/// The request is refused with this reason. It is not retryable: asking again does not make the value appear.
/// </remarks>
/// <param name="name">The name of the value.</param>
/// <param name="innerException">The cause, if any.</param>
public class RequiredContextValueAbsentException(string name, Exception? innerException = null)
    : Exception($"The required context value '{name}' is absent.", innerException)
{
    /// <summary>
    /// Gets the name of the value.
    /// </summary>
    public string Name { get; } = name;
}
