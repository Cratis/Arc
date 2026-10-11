// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// The exception that is thrown when a server-generated value fails validation.
/// </summary>
/// <param name="commandType">The command declaring the generated value.</param>
/// <param name="name">The generated value name.</param>
/// <param name="valueType">The declared value type.</param>
/// <param name="failures">The validator failures, which may contain sensitive values and must not be logged.</param>
public class GeneratedValueRejected(Type commandType, string name, Type valueType, IReadOnlyList<string> failures)
    : Exception($"Generated value '{name}' of type '{valueType.FullName}' for command '{commandType.FullName}' was rejected by its validator.")
{
    /// <summary>
    /// Gets the validator failures. These may contain sensitive values and must not be logged or traced.
    /// </summary>
    public IReadOnlyList<string> Failures { get; } = failures;
}
