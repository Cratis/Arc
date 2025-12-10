// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Execution;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the result coming from performing a query.
/// </summary>
public class QueryResult
{
    /// <summary>
    /// Gets or inits the <see cref="PagingInfo"/> for the query.
    /// </summary>
    public PagingInfo Paging { get; set; } = PagingInfo.NotPaged;

    /// <summary>
    /// Gets the <see cref="CorrelationId"/> associated with the command.
    /// </summary>
    public CorrelationId CorrelationId { get; set; } = new(Guid.Empty);

    /// <summary>
    /// The data returned.
    /// </summary>
    public object Data { get; set; } = default!;

    /// <summary>
    /// Gets whether or not the query executed successfully.
    /// </summary>
    public bool IsSuccess => IsAuthorized && IsValid && !HasExceptions;

    /// <summary>
    /// Gets whether the query was authorized to execute.
    /// </summary>
    public bool IsAuthorized { get; set; } = true;

    /// <summary>
    /// Gets whether the query is valid.
    /// </summary>
    public bool IsValid => !ValidationResults.Any();

    /// <summary>
    /// Gets whether there are any exceptions that occurred.
    /// </summary>
    public bool HasExceptions => ExceptionMessages.Any();

    /// <summary>
    /// Gets the validation results for the query.
    /// </summary>
    public IEnumerable<ValidationResult> ValidationResults { get; set; } = [];

    /// <summary>
    /// Gets any exception messages that might have occurred.
    /// </summary>
    public IEnumerable<string> ExceptionMessages { get; set; } = [];

    /// <summary>
    /// Gets the stack trace if there was an exception.
    /// </summary>
    public string ExceptionStackTrace { get; set; } = string.Empty;

    /// <summary>
    /// Represents a successful command result.
    /// </summary>
    /// <param name="correlationId">The correlation ID.</param>
    /// <returns>A successful <see cref="QueryResult"/>.</returns>
    public static QueryResult Success(CorrelationId correlationId) => new() { CorrelationId = correlationId };

    /// <summary>
    /// Creates a new <see cref="QueryResult"/> representing an unauthorized query execution.
    /// </summary>
    /// <param name="correlationId">The <see cref="CorrelationId"/> associated with the query.</param>
    /// <returns>A <see cref="QueryResult"/>.</returns>
    public static QueryResult Unauthorized(CorrelationId correlationId) => new() { CorrelationId = correlationId, IsAuthorized = false };

    /// <summary>
    /// Creates a new <see cref="QueryResult"/> representing a missing performer.
    /// </summary>
    /// <param name="correlationId">The <see cref="CorrelationId"/> associated with the query.</param>
    /// <param name="name">The name of the query that is missing a performer.</param>
    /// <returns>A <see cref="QueryResult"/>.</returns>
    public static QueryResult MissingPerformer(CorrelationId correlationId, FullyQualifiedQueryName name) => new() { CorrelationId = correlationId, ExceptionMessages = [$"No performer found for query {name}"] };

    /// <summary>
    /// Creates a new <see cref="QueryResult"/> representing an error.
    /// </summary>
    /// <param name="correlationId">The <see cref="CorrelationId"/> associated with the query.</param>
    /// <param name="message">The error message.</param>
    /// <returns>A <see cref="QueryResult"/>.</returns>
    public static QueryResult Error(CorrelationId correlationId, string message) => new() { CorrelationId = correlationId, ExceptionMessages = [message] };

    /// <summary>
    /// Creates a new <see cref="QueryResult"/> representing an error.
    /// </summary>
    /// <param name="correlationId">The <see cref="CorrelationId"/> associated with the query.</param>
    /// <param name="exception">The exception.</param>
    /// <returns>A <see cref="QueryResult"/>.</returns>
    public static QueryResult Error(CorrelationId correlationId, Exception exception) => new() { CorrelationId = correlationId, ExceptionMessages = [exception.Message], ExceptionStackTrace = exception.StackTrace ?? string.Empty };

    /// <summary>
    /// Merges the results of one or more <see cref="QueryResult"/> instances into this.
    /// </summary>
    /// <param name="queryResults">Params of <see cref="QueryResult"/> to merge with.</param>
    public void MergeWith(params QueryResult[] queryResults)
    {
        IsAuthorized = IsAuthorized && queryResults.All(r => r.IsAuthorized);
        ValidationResults = [.. ValidationResults, .. queryResults.SelectMany(r => r.ValidationResults)];
        ExceptionMessages = [.. ExceptionMessages, .. queryResults.SelectMany(r => r.ExceptionMessages)];
        ExceptionStackTrace = string.Join(Environment.NewLine, new[] { ExceptionStackTrace }.Concat(queryResults.Select(r => r.ExceptionStackTrace)));
        Data = queryResults.Select(r => r.Data).FirstOrDefault() ?? Data;
        if (ExceptionStackTrace.StartsWith(Environment.NewLine))
        {
            ExceptionStackTrace = ExceptionStackTrace[Environment.NewLine.Length..];
        }
    }
}
