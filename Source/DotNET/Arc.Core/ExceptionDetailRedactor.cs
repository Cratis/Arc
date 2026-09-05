// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Execution;
using Microsoft.Extensions.Logging;

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

    static void LogFullDetail(ILogger logger, CorrelationId correlationId, IEnumerable<string> messages, string stackTrace) =>
        logger.RedactedExceptionDetail(correlationId.ToString(), string.Join("; ", messages), stackTrace);
}
