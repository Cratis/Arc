// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc;

/// <summary>
/// Redacts exception detail (messages and stack traces) from command and query results before they are
/// serialized to a client, so internal information is not leaked outside of the Development environment.
/// </summary>
/// <remarks>
/// Redaction mutates the result that is about to be written to the wire; the full detail is logged
/// server-side first so nothing is lost. The correlation identifier is retained and a generic marker
/// replaces the real messages so that <c>HasExceptions</c> (and therefore the HTTP status code) is unchanged.
/// </remarks>
public static class ExceptionDetailRedactor
{
    /// <summary>
    /// The generic message that replaces real exception detail when redaction is enabled.
    /// </summary>
    public const string RedactedMessage = "An internal error occurred while processing the request. See server logs for details.";

    /// <summary>
    /// Redacts exception detail from a <see cref="CommandResult"/> when exposing detail is disabled.
    /// </summary>
    /// <param name="result">The <see cref="CommandResult"/> to redact.</param>
    /// <param name="exposeExceptionDetails">Whether exception detail may be exposed to the client.</param>
    /// <param name="logger">The <see cref="ILogger"/> used to log the full detail server-side.</param>
    public static void Redact(CommandResult result, bool exposeExceptionDetails, ILogger logger)
    {
        if (exposeExceptionDetails || !result.HasExceptions)
        {
            return;
        }

        LogFullDetail(logger, result.CorrelationId, result.ExceptionMessages, result.ExceptionStackTrace);
        result.ExceptionMessages = [RedactedMessage];
        result.ExceptionStackTrace = string.Empty;
    }

    /// <summary>
    /// Redacts exception detail from a <see cref="QueryResult"/> when exposing detail is disabled.
    /// </summary>
    /// <param name="result">The <see cref="QueryResult"/> to redact.</param>
    /// <param name="exposeExceptionDetails">Whether exception detail may be exposed to the client.</param>
    /// <param name="logger">The <see cref="ILogger"/> used to log the full detail server-side.</param>
    public static void Redact(QueryResult result, bool exposeExceptionDetails, ILogger logger)
    {
        if (exposeExceptionDetails || !result.HasExceptions)
        {
            return;
        }

        LogFullDetail(logger, result.CorrelationId, result.ExceptionMessages, result.ExceptionStackTrace);
        result.ExceptionMessages = [RedactedMessage];
        result.ExceptionStackTrace = string.Empty;
    }

    /// <summary>
    /// Resolves whether exception detail may be exposed by the request's host.
    /// </summary>
    /// <param name="services">The request services.</param>
    /// <returns>Whether the host explicitly exposes exception detail.</returns>
    internal static bool ExposeFor(IServiceProvider? services) => services?.GetService<IOptions<ArcOptions>>()?.Value.ExposeExceptionDetails ?? false;

    /// <summary>
    /// Gets the message to send for an observable query failure.
    /// </summary>
    /// <param name="error">The failure already logged on the server.</param>
    /// <param name="exposeExceptionDetails">Whether to expose its message.</param>
    /// <returns>The message to send.</returns>
    internal static string MessageFor(Exception error, bool exposeExceptionDetails) => exposeExceptionDetails ? error.Message : RedactedMessage;

    static void LogFullDetail(ILogger logger, CorrelationId correlationId, IEnumerable<string> messages, string stackTrace) =>
        logger.RedactedExceptionDetail(correlationId.ToString(), string.Join("; ", messages), stackTrace);
}
