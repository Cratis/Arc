// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Immutable generated invocation metadata. Both execution and recovery arguments are preflighted before any operation starts.
/// </summary>
/// <param name="executeParameters">Execution parameter types in signature order.</param>
/// <param name="execute">Typed execution call.</param>
/// <param name="compensateParameters">Recovery parameter types in signature order.</param>
/// <param name="compensate">Optional typed recovery call.</param>
public sealed class CommandOperationInvoker(
    IEnumerable<Type> executeParameters,
    CommandOperationMethod execute,
    IEnumerable<Type> compensateParameters,
    CommandOperationMethod? compensate)
{
    /// <summary>
    /// Gets execution argument types in call order.
    /// </summary>
    internal IReadOnlyList<Type> ExecuteParameters { get; } = Array.AsReadOnly(executeParameters.ToArray());

    /// <summary>
    /// Gets recovery argument types in call order.
    /// </summary>
    internal IReadOnlyList<Type> CompensateParameters { get; } = Array.AsReadOnly(compensateParameters.ToArray());

    /// <summary>
    /// Gets the execution call.
    /// </summary>
    internal CommandOperationMethod Execute { get; } = execute;

    /// <summary>
    /// Gets the optional recovery call.
    /// </summary>
    internal CommandOperationMethod? Compensate { get; } = compensate;
}
