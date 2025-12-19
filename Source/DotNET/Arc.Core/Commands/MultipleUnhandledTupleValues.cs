// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// The exception that is thrown when multiple values in a tuple don't have corresponding response value handlers.
/// </summary>
/// <remarks>
/// When a command handler returns a tuple with multiple values, each value is processed to determine if there's
/// a corresponding response value handler. If exactly one value doesn't have a handler, it becomes the response.
/// If multiple values don't have handlers, this exception is thrown as the system cannot determine which value
/// should be the response.
/// </remarks>
/// <param name="unhandledValues">The values that don't have corresponding response value handlers.</param>
public class MultipleUnhandledTupleValues(IEnumerable<object> unhandledValues)
    : Exception($"Multiple values in the tuple don't have corresponding response value handlers. Cannot determine which value should be the response. Unhandled values: {string.Join(", ", unhandledValues.Select(v => v?.GetType().Name ?? "null"))}")
{
    /// <summary>
    /// Gets the values that don't have corresponding response value handlers.
    /// </summary>
    public IReadOnlyCollection<object> UnhandledValues { get; } = unhandledValues.ToArray();
}