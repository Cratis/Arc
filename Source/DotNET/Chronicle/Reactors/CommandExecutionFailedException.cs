// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Reactors;

/// <summary>
/// Exception thrown when a command execution fails within a reactor.
/// </summary>
/// <param name="commandType">The type of the command that failed.</param>
/// <param name="result">The <see cref="CommandResult"/> from the failed command execution.</param>
public class CommandExecutionFailedException(Type commandType, CommandResult result) : Exception(
    $"Command of type '{commandType.Name}' execution failed. " +
    $"IsAuthorized: {result.IsAuthorized}, " +
    $"ValidationResults: {result.ValidationResults.Count()}, " +
    $"ExceptionMessages: {result.ExceptionMessages.Count()}")
{
    /// <summary>
    /// Gets the type of the command that failed.
    /// </summary>
    public Type CommandType { get; } = commandType;

    /// <summary>
    /// Gets the <see cref="CommandResult"/> from the failed command execution.
    /// </summary>
    public CommandResult Result { get; } = result;
}
