// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands;

/// <summary>
/// Defines a system can execute commands.
/// </summary>
public interface ICommandPipeline
{
    /// <summary>
    /// Executes the given command, creating a dedicated service scope for dependency resolution.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="allowedSeverity">Optional maximum validation result severity level to allow.</param>
    /// <returns>A <see cref="CommandResult"/> representing the result of executing the command.</returns>
    Task<CommandResult> Execute(object command, ValidationResultSeverity? allowedSeverity = default);

    /// <summary>
    /// Executes the given command within the provided service scope.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> scoped to the current request, used to resolve handler dependencies.</param>
    /// <param name="allowedSeverity">Optional maximum validation result severity level to allow.</param>
    /// <returns>A <see cref="CommandResult"/> representing the result of executing the command.</returns>
    Task<CommandResult> Execute(object command, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity = default);

    /// <summary>
    /// Executes the given command and returns a strongly-typed result, creating a dedicated service scope for dependency resolution.
    /// </summary>
    /// <typeparam name="TResult">The type of the response returned by the command handler.</typeparam>
    /// <param name="command">The command to execute.</param>
    /// <param name="allowedSeverity">Optional maximum validation result severity level to allow.</param>
    /// <returns>A <see cref="CommandResult{TResult}"/> representing the result of executing the command.</returns>
    /// <remarks>
    /// If the handler returns a response of a different type than <typeparamref name="TResult"/>, an <see cref="InvalidCastException"/> is thrown.
    /// If the handler returns no response, or the command fails (authorization, validation, exception, no handler), the result is returned with <see cref="CommandResult{TResult}.Response"/> set to <see langword="default"/>.
    /// </remarks>
    Task<CommandResult<TResult>> Execute<TResult>(object command, ValidationResultSeverity? allowedSeverity = default);

    /// <summary>
    /// Executes the given command and returns a strongly-typed result within the provided service scope.
    /// </summary>
    /// <typeparam name="TResult">The type of the response returned by the command handler.</typeparam>
    /// <param name="command">The command to execute.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> scoped to the current request, used to resolve handler dependencies.</param>
    /// <param name="allowedSeverity">Optional maximum validation result severity level to allow.</param>
    /// <returns>A <see cref="CommandResult{TResult}"/> representing the result of executing the command.</returns>
    /// <remarks>
    /// If the handler returns a response of a different type than <typeparamref name="TResult"/>, an <see cref="InvalidCastException"/> is thrown.
    /// If the handler returns no response, or the command fails (authorization, validation, exception, no handler), the result is returned with <see cref="CommandResult{TResult}.Response"/> set to <see langword="default"/>.
    /// </remarks>
    Task<CommandResult<TResult>> Execute<TResult>(object command, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity = default);

    /// <summary>
    /// Validates the given command without executing it, creating a dedicated service scope for dependency resolution.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <param name="allowedSeverity">Optional maximum validation result severity level to allow.</param>
    /// <returns>A <see cref="CommandResult"/> representing the validation result.</returns>
    /// <remarks>
    /// This method runs authorization and validation filters but does not invoke the command handler.
    /// Use this for pre-flight validation before executing a command.
    /// </remarks>
    Task<CommandResult> Validate(object command, ValidationResultSeverity? allowedSeverity = default);

    /// <summary>
    /// Validates the given command without executing it, within the provided service scope.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> scoped to the current request, used to resolve handler dependencies.</param>
    /// <param name="allowedSeverity">Optional maximum validation result severity level to allow.</param>
    /// <returns>A <see cref="CommandResult"/> representing the validation result.</returns>
    /// <remarks>
    /// This method runs authorization and validation filters but does not invoke the command handler.
    /// Use this for pre-flight validation before executing a command.
    /// </remarks>
    Task<CommandResult> Validate(object command, IServiceProvider serviceProvider, ValidationResultSeverity? allowedSeverity = default);
}
