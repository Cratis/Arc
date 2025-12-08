// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Execution;

namespace Cratis.Arc.Commands;

#pragma warning disable SA1402 // File may only contain a single type

/// <summary>
/// Represents the result coming from executing a command.
/// </summary>
public class CommandResult
{
    /// <summary>
    /// Gets the <see cref="CorrelationId"/> associated with the command.
    /// </summary>
    public CorrelationId CorrelationId { get; init; } = CorrelationId.NotSet;

    /// <summary>
    /// Gets whether or not the command executed successfully.
    /// </summary>
    public bool IsSuccess => IsAuthorized && IsValid && !HasExceptions;

    /// <summary>
    /// Gets whether or not the command was authorized to execute.
    /// </summary>
    public bool IsAuthorized { get; set; } = true;

    /// <summary>
    /// Gets whether or not the command is valid.
    /// </summary>
    public bool IsValid => !ValidationResults.Any();

    /// <summary>
    /// Gets or sets whether or not there are any exceptions that occurred.
    /// </summary>
    public bool HasExceptions => ExceptionMessages.Any();

    /// <summary>
    /// Gets or sets any validation result.
    /// </summary>
    public IEnumerable<ValidationResult> ValidationResults { get; set; } = [];

    /// <summary>
    /// Gets or sets any exception messages that might have occurred.
    /// </summary>
    public IEnumerable<string> ExceptionMessages { get; set; } = [];

    /// <summary>
    /// Gets or sets the stack trace if there was an exception.
    /// </summary>
    public string ExceptionStackTrace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for authorization failure, if any.
    /// </summary>
    public string AuthorizationFailureReason { get; set; } = string.Empty;

    /// <summary>
    /// Creates a new <see cref="CommandResult"/> representing a successful command execution.
    /// </summary>
    /// <param name="correlationId">The <see cref="CorrelationId"/> associated with the command.</param>
    /// <returns>A <see cref="CommandResult"/>.</returns>
    public static CommandResult Success(CorrelationId correlationId) => new() { CorrelationId = correlationId };

    /// <summary>
    /// Creates a new <see cref="CommandResult"/> representing an unauthorized command execution.
    /// </summary>
    /// <param name="correlationId">The <see cref="CorrelationId"/> associated with the command.</param>
    /// <param name="reason">Optional reason for the authorization failure.</param>
    /// <returns>A <see cref="CommandResult"/>.</returns>
    public static CommandResult Unauthorized(CorrelationId correlationId, string? reason = default) => new() { CorrelationId = correlationId, IsAuthorized = false, AuthorizationFailureReason = reason ?? string.Empty };

    /// <summary>
    /// Creates a new <see cref="CommandResult"/> representing a missing handler.
    /// </summary>
    /// <param name="correlationId">The <see cref="CorrelationId"/> associated with the command.</param>
    /// <param name="type">The type of command that is missing a handler.</param>
    /// <returns>A <see cref="CommandResult"/>.</returns>
    public static CommandResult MissingHandler(CorrelationId correlationId, Type type) => new() { CorrelationId = correlationId, ExceptionMessages = [$"No handler found for command of type {type}"] };

    /// <summary>
    /// Creates a new <see cref="CommandResult"/> representing an error.
    /// </summary>
    /// <param name="correlationId">The <see cref="CorrelationId"/> associated with the command.</param>
    /// <param name="message">The error message.</param>
    /// <returns>A <see cref="CommandResult"/>.</returns>
    public static CommandResult Error(CorrelationId correlationId, string message) => new() { CorrelationId = correlationId, ExceptionMessages = [message] };

    /// <summary>
    /// Creates a new <see cref="CommandResult"/> representing an error.
    /// </summary>
    /// <param name="correlationId">The <see cref="CorrelationId"/> associated with the command.</param>
    /// <param name="exception">The exception.</param>
    /// <returns>A <see cref="CommandResult"/>.</returns>
    public static CommandResult Error(CorrelationId correlationId, Exception exception) => new() { CorrelationId = correlationId, ExceptionMessages = [exception.Message], ExceptionStackTrace = exception.StackTrace ?? string.Empty };

    /// <summary>
    /// Merges the results of one or more <see cref="CommandResult"/> instances into this.
    /// </summary>
    /// <param name="commandResults">Params of <see cref="CommandResult"/> to merge with.</param>
    public void MergeWith(params CommandResult[] commandResults)
    {
        IsAuthorized = IsAuthorized && commandResults.All(r => r.IsAuthorized);
        ValidationResults = [.. ValidationResults, .. commandResults.SelectMany(r => r.ValidationResults)];
        ExceptionMessages = [.. ExceptionMessages, .. commandResults.SelectMany(r => r.ExceptionMessages)];
        ExceptionStackTrace = string.Join(Environment.NewLine, new[] { ExceptionStackTrace }.Concat(commandResults.Select(r => r.ExceptionStackTrace)));
        if (ExceptionStackTrace.StartsWith(Environment.NewLine))
        {
            ExceptionStackTrace = ExceptionStackTrace[Environment.NewLine.Length..];
        }
    }
}

/// <summary>
/// Represents the result coming from executing a command with a response.
/// </summary>
/// <typeparam name="TResponse">Type of the data returned.</typeparam>
public class CommandResult<TResponse> : CommandResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandResult{T}"/> class.
    /// </summary>
    public CommandResult()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandResult{T}"/> class.
    /// </summary>
    /// <param name="response">The response.</param>
    public CommandResult(TResponse? response)
    {
        Response = response;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandResult{T}"/> class.
    /// </summary>
    /// <param name="correlationId">The <see cref="CorrelationId"/> associated with the command.</param>
    /// <param name="response">The response.</param>
    public CommandResult(CorrelationId correlationId, TResponse? response)
    {
        CorrelationId = correlationId;
        Response = response;
    }

    /// <summary>
    /// Gets or sets the optional response object that will be returned from the command handler.
    /// </summary>
    public TResponse? Response { get; set; }

    /// <summary>
    /// Creates a new <see cref="CommandResult"/> representing a successful command execution.
    /// </summary>
    /// <param name="correlationId">The <see cref="CorrelationId"/> associated with the command.</param>
    /// <returns>A <see cref="CommandResult{T}"/>.</returns>
    public static new CommandResult<TResponse> Success(CorrelationId correlationId) => new() { CorrelationId = correlationId };
}
