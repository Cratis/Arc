// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands;

/// <summary>
/// Extension methods for executing commands with an explicit cancellation token.
/// </summary>
public static class CommandPipelineCancellationExtensions
{
    /// <summary>
    /// Executes the given command, creating a dedicated service scope for dependency resolution.
    /// </summary>
    /// <param name="pipeline">The <see cref="ICommandPipeline"/>.</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">The cancellation token for the command execution.</param>
    /// <returns>A <see cref="CommandResult"/> representing the result of executing the command.</returns>
    public static Task<CommandResult> Execute(this ICommandPipeline pipeline, object command, CancellationToken cancellationToken) =>
        pipeline.Execute(command, (ValidationResultSeverity?)default, cancellationToken);

    /// <summary>
    /// Executes the given command, creating a dedicated service scope for dependency resolution.
    /// </summary>
    /// <param name="pipeline">The <see cref="ICommandPipeline"/>.</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="allowedSeverity">Optional maximum validation result severity level to allow.</param>
    /// <param name="cancellationToken">The cancellation token for the command execution.</param>
    /// <returns>A <see cref="CommandResult"/> representing the result of executing the command.</returns>
    public static Task<CommandResult> Execute(
        this ICommandPipeline pipeline,
        object command,
        ValidationResultSeverity? allowedSeverity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        return pipeline is ICommandPipelineWithCancellation cancellationAwarePipeline
            ? cancellationAwarePipeline.Execute(command, allowedSeverity, cancellationToken)
            : pipeline.Execute(command, allowedSeverity);
    }

    /// <summary>
    /// Executes the given command within the provided service scope.
    /// </summary>
    /// <param name="pipeline">The <see cref="ICommandPipeline"/>.</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> scoped to the current request, used to resolve handler dependencies.</param>
    /// <param name="cancellationToken">The cancellation token for the command execution.</param>
    /// <returns>A <see cref="CommandResult"/> representing the result of executing the command.</returns>
    public static Task<CommandResult> Execute(this ICommandPipeline pipeline, object command, IServiceProvider serviceProvider, CancellationToken cancellationToken) =>
        pipeline.Execute(command, serviceProvider, default, cancellationToken);

    /// <summary>
    /// Executes the given command within the provided service scope.
    /// </summary>
    /// <param name="pipeline">The <see cref="ICommandPipeline"/>.</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> scoped to the current request, used to resolve handler dependencies.</param>
    /// <param name="allowedSeverity">Optional maximum validation result severity level to allow.</param>
    /// <param name="cancellationToken">The cancellation token for the command execution.</param>
    /// <returns>A <see cref="CommandResult"/> representing the result of executing the command.</returns>
    public static Task<CommandResult> Execute(
        this ICommandPipeline pipeline,
        object command,
        IServiceProvider serviceProvider,
        ValidationResultSeverity? allowedSeverity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        return pipeline is ICommandPipelineWithCancellation cancellationAwarePipeline
            ? cancellationAwarePipeline.Execute(command, serviceProvider, allowedSeverity, cancellationToken)
            : pipeline.Execute(command, serviceProvider, allowedSeverity);
    }

    /// <summary>
    /// Executes the given command and returns a strongly-typed result, creating a dedicated service scope for dependency resolution.
    /// </summary>
    /// <typeparam name="TResult">The type of the response returned by the command handler.</typeparam>
    /// <param name="pipeline">The <see cref="ICommandPipeline"/>.</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">The cancellation token for the command execution.</param>
    /// <returns>A <see cref="CommandResult{TResult}"/> representing the result of executing the command.</returns>
    public static Task<CommandResult<TResult>> Execute<TResult>(this ICommandPipeline pipeline, object command, CancellationToken cancellationToken) =>
        pipeline.Execute<TResult>(command, (ValidationResultSeverity?)default, cancellationToken);

    /// <summary>
    /// Executes the given command and returns a strongly-typed result, creating a dedicated service scope for dependency resolution.
    /// </summary>
    /// <typeparam name="TResult">The type of the response returned by the command handler.</typeparam>
    /// <param name="pipeline">The <see cref="ICommandPipeline"/>.</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="allowedSeverity">Optional maximum validation result severity level to allow.</param>
    /// <param name="cancellationToken">The cancellation token for the command execution.</param>
    /// <returns>A <see cref="CommandResult{TResult}"/> representing the result of executing the command.</returns>
    public static Task<CommandResult<TResult>> Execute<TResult>(
        this ICommandPipeline pipeline,
        object command,
        ValidationResultSeverity? allowedSeverity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        return pipeline is ICommandPipelineWithCancellation cancellationAwarePipeline
            ? cancellationAwarePipeline.Execute<TResult>(command, allowedSeverity, cancellationToken)
            : pipeline.Execute<TResult>(command, allowedSeverity);
    }

    /// <summary>
    /// Executes the given command and returns a strongly-typed result within the provided service scope.
    /// </summary>
    /// <typeparam name="TResult">The type of the response returned by the command handler.</typeparam>
    /// <param name="pipeline">The <see cref="ICommandPipeline"/>.</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> scoped to the current request, used to resolve handler dependencies.</param>
    /// <param name="cancellationToken">The cancellation token for the command execution.</param>
    /// <returns>A <see cref="CommandResult{TResult}"/> representing the result of executing the command.</returns>
    public static Task<CommandResult<TResult>> Execute<TResult>(this ICommandPipeline pipeline, object command, IServiceProvider serviceProvider, CancellationToken cancellationToken) =>
        pipeline.Execute<TResult>(command, serviceProvider, default, cancellationToken);

    /// <summary>
    /// Executes the given command and returns a strongly-typed result within the provided service scope.
    /// </summary>
    /// <typeparam name="TResult">The type of the response returned by the command handler.</typeparam>
    /// <param name="pipeline">The <see cref="ICommandPipeline"/>.</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> scoped to the current request, used to resolve handler dependencies.</param>
    /// <param name="allowedSeverity">Optional maximum validation result severity level to allow.</param>
    /// <param name="cancellationToken">The cancellation token for the command execution.</param>
    /// <returns>A <see cref="CommandResult{TResult}"/> representing the result of executing the command.</returns>
    public static Task<CommandResult<TResult>> Execute<TResult>(
        this ICommandPipeline pipeline,
        object command,
        IServiceProvider serviceProvider,
        ValidationResultSeverity? allowedSeverity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        return pipeline is ICommandPipelineWithCancellation cancellationAwarePipeline
            ? cancellationAwarePipeline.Execute<TResult>(command, serviceProvider, allowedSeverity, cancellationToken)
            : pipeline.Execute<TResult>(command, serviceProvider, allowedSeverity);
    }

    /// <summary>
    /// Validates the given command without executing it, creating a dedicated service scope for dependency resolution.
    /// </summary>
    /// <param name="pipeline">The <see cref="ICommandPipeline"/>.</param>
    /// <param name="command">The command to validate.</param>
    /// <param name="cancellationToken">The cancellation token for the command validation.</param>
    /// <returns>A <see cref="CommandResult"/> representing the validation result.</returns>
    public static Task<CommandResult> Validate(this ICommandPipeline pipeline, object command, CancellationToken cancellationToken) =>
        pipeline.Validate(command, (ValidationResultSeverity?)default, cancellationToken);

    /// <summary>
    /// Validates the given command without executing it, creating a dedicated service scope for dependency resolution.
    /// </summary>
    /// <param name="pipeline">The <see cref="ICommandPipeline"/>.</param>
    /// <param name="command">The command to validate.</param>
    /// <param name="allowedSeverity">Optional maximum validation result severity level to allow.</param>
    /// <param name="cancellationToken">The cancellation token for the command validation.</param>
    /// <returns>A <see cref="CommandResult"/> representing the validation result.</returns>
    public static Task<CommandResult> Validate(
        this ICommandPipeline pipeline,
        object command,
        ValidationResultSeverity? allowedSeverity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        return pipeline is ICommandPipelineWithCancellation cancellationAwarePipeline
            ? cancellationAwarePipeline.Validate(command, allowedSeverity, cancellationToken)
            : pipeline.Validate(command, allowedSeverity);
    }

    /// <summary>
    /// Validates the given command without executing it, within the provided service scope.
    /// </summary>
    /// <param name="pipeline">The <see cref="ICommandPipeline"/>.</param>
    /// <param name="command">The command to validate.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> scoped to the current request, used to resolve handler dependencies.</param>
    /// <param name="cancellationToken">The cancellation token for the command validation.</param>
    /// <returns>A <see cref="CommandResult"/> representing the validation result.</returns>
    public static Task<CommandResult> Validate(this ICommandPipeline pipeline, object command, IServiceProvider serviceProvider, CancellationToken cancellationToken) =>
        pipeline.Validate(command, serviceProvider, default, cancellationToken);

    /// <summary>
    /// Validates the given command without executing it, within the provided service scope.
    /// </summary>
    /// <param name="pipeline">The <see cref="ICommandPipeline"/>.</param>
    /// <param name="command">The command to validate.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> scoped to the current request, used to resolve handler dependencies.</param>
    /// <param name="allowedSeverity">Optional maximum validation result severity level to allow.</param>
    /// <param name="cancellationToken">The cancellation token for the command validation.</param>
    /// <returns>A <see cref="CommandResult"/> representing the validation result.</returns>
    public static Task<CommandResult> Validate(
        this ICommandPipeline pipeline,
        object command,
        IServiceProvider serviceProvider,
        ValidationResultSeverity? allowedSeverity,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        return pipeline is ICommandPipelineWithCancellation cancellationAwarePipeline
            ? cancellationAwarePipeline.Validate(command, serviceProvider, allowedSeverity, cancellationToken)
            : pipeline.Validate(command, serviceProvider, allowedSeverity);
    }
}
