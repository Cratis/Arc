// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Contexts;

/// <summary>
/// Exception that gets thrown when the context values cannot be read as the requested type.
/// </summary>
/// <param name="type">The requested type.</param>
/// <param name="reason">Why the values do not fit the type.</param>
public class ContextValuesTypeMismatch(Type type, string reason)
    : Exception($"The context values cannot be read as '{type}': {reason}")
{
    /// <summary>
    /// Gets the requested type.
    /// </summary>
    public Type RequestedType { get; } = type;
}
