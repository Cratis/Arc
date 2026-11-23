// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Arc.Validation;
using Cratis.Execution;
using Cratis.Types;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Exceptions;

/// <summary>Applies opt-in mappings once, at an output boundary.</summary>
internal static class ExceptionResponseMapping
{
    /// <summary>Maps an eligible command failure and redacts private detail.</summary>
    /// <param name="result">The command result.</param>
    /// <param name="services">The active request or subscription services.</param>
    /// <param name="logger">The boundary logger.</param>
    internal static void Apply(CommandResult result, IServiceProvider services, ILogger logger)
    {
        var response = Resolve(result.ExceptionResponseCandidate, result.HasExceptions && result.IsAuthorized && result.IsValid && !result.IsRetryable, services, logger);
        result.ErrorCode = response?.ErrorCode;
        if (response is not null) ExceptionDetailRedactor.Redact(result, false, logger);
    }

    /// <summary>Maps an eligible query failure and redacts private detail.</summary>
    /// <param name="result">The query result.</param>
    /// <param name="services">The active request or subscription services.</param>
    /// <param name="logger">The boundary logger.</param>
    internal static void Apply(QueryResult result, IServiceProvider services, ILogger logger)
    {
        var response = Resolve(result.ExceptionResponseCandidate, result.HasExceptions && result.IsAuthorized && result.IsValid && !result.IsRetryable && result.IsReady, services, logger);
        result.ErrorCode = response?.ErrorCode;
        if (response is not null) ExceptionDetailRedactor.Redact(result, false, logger);
    }

    /// <summary>Gets a command mapping unless a built-in failure takes precedence.</summary>
    /// <param name="result">The command result.</param>
    /// <returns>The accepted response, if eligible.</returns>
    internal static ExceptionResponse? ResponseFor(CommandResult result) =>
        result.HasExceptions && result.IsAuthorized && result.IsValid && !result.IsRetryable ? result.ExceptionResponseCandidate?.Response : null;

    /// <summary>Gets a query mapping unless a built-in failure takes precedence.</summary>
    /// <param name="result">The query result.</param>
    /// <returns>The accepted response, if eligible.</returns>
    internal static ExceptionResponse? ResponseFor(QueryResult result) =>
        result.HasExceptions && result.IsAuthorized && result.IsValid && !result.IsRetryable && result.IsReady ? result.ExceptionResponseCandidate?.Response : null;

    /// <summary>Creates a safe stream failure without changing the transport status or headers.</summary>
    /// <param name="correlationId">The subscription correlation.</param>
    /// <param name="exception">The original failure.</param>
    /// <param name="services">The subscription services.</param>
    /// <param name="logger">The boundary logger.</param>
    /// <param name="exposeExceptionDetails">Whether unmatched failures expose their original message.</param>
    /// <returns>The mapped failure, or the unchanged legacy message-only failure.</returns>
    internal static QueryResult StreamError(CorrelationId correlationId, Exception exception, IServiceProvider services, ILogger logger, bool exposeExceptionDetails)
    {
        var result = QueryResult.Error(correlationId, exception);
        Apply(result, services, logger);
        return result.ErrorCode is not null ? result : QueryResult.Error(correlationId, ExceptionDetailRedactor.MessageFor(exception, exposeExceptionDetails));
    }

    static ExceptionResponse? Resolve(ExceptionResponseCandidate? candidate, bool isEligible, IServiceProvider services, ILogger logger)
    {
        if (!isEligible) return null;
        if (candidate is null) return null;
        if (IsBuiltIn(candidate.Exception)) return null;
        if (candidate.IsAttempted) return candidate.Response;
        candidate.IsAttempted = true;
        try
        {
            candidate.Response = Map(candidate.Exception, services, logger);
        }
        catch (Exception exception)
        {
            logger.ResponseMappingFailed(exception);
        }
        return candidate.Response;
    }

    static bool IsBuiltIn(Exception exception) => exception is IValidationFailure || UnauthorizedCause.IsCauseOf(exception);

    static ExceptionResponse? Map(Exception exception, IServiceProvider services, ILogger logger)
    {
        if (services.GetService(typeof(IInstancesOf<IExceptionResponseMapper>)) is not IInstancesOf<IExceptionResponseMapper> mappers) return null;
        foreach (var mapper in mappers)
        {
            if (!mapper.TryMap(exception, out var response)) continue;
            if (response is not null && ExceptionResponseValidation.IsValid(response)) return response;
            logger.InvalidExceptionResponse();
            return null;
        }
        return null;
    }
}
