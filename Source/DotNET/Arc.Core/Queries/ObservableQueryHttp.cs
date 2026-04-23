// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Linq;
using System.Net;

namespace Cratis.Arc.Queries;

/// <summary>
/// Provides shared HTTP handling for observable queries.
/// </summary>
public static class ObservableQueryHttp
{
    /// <summary>
    /// The query string key used to wait for the first result before returning the HTTP response.
    /// </summary>
    public const string WaitForFirstResultQueryStringKey = "waitForFirstResult";

    /// <summary>
    /// The query string key used to override the wait-for-first-result timeout in seconds.
    /// </summary>
    public const string WaitForFirstResultTimeoutQueryStringKey = "waitForFirstResultTimeout";

    /// <summary>
    /// The default timeout used when waiting for the first observable query result.
    /// </summary>
    public static readonly TimeSpan DefaultWaitForFirstResultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets the observable query HTTP options from the provided query string values.
    /// </summary>
    /// <param name="query">The query string values.</param>
    /// <returns>The parsed <see cref="ObservableQueryHttpOptions"/>.</returns>
    public static ObservableQueryHttpOptions GetOptions(IReadOnlyDictionary<string, string> query)
    {
        var waitForFirstResult =
            TryGetValue(query, WaitForFirstResultQueryStringKey, out var waitValue) &&
            bool.TryParse(waitValue, out var parsedWaitValue) &&
            parsedWaitValue;

        var waitForFirstResultTimeout = DefaultWaitForFirstResultTimeout;

        if (TryGetValue(query, WaitForFirstResultTimeoutQueryStringKey, out var timeoutValue) &&
            double.TryParse(timeoutValue, CultureInfo.InvariantCulture, out var timeoutInSeconds) &&
            timeoutInSeconds > 0)
        {
            waitForFirstResultTimeout = TimeSpan.FromSeconds(timeoutInSeconds);
        }

        return new(waitForFirstResult, waitForFirstResultTimeout);
    }

    /// <summary>
    /// Creates the HTTP response for an observable query result.
    /// </summary>
    /// <param name="queryContext">The <see cref="QueryContext"/> for the current request.</param>
    /// <param name="streamingData">The observable query result.</param>
    /// <param name="options">The <see cref="ObservableQueryHttpOptions"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> for the request.</param>
    /// <returns>The <see cref="ObservableQueryHttpResponse"/> to send.</returns>
    public static async Task<ObservableQueryHttpResponse> CreateResponse(
        QueryContext queryContext,
        object streamingData,
        ObservableQueryHttpOptions options,
        CancellationToken cancellationToken)
    {
        var observableInterface = GetObservableInterfaceFor(streamingData.GetType());
        if (observableInterface is null)
        {
            return new(CreateSuccessResult(queryContext, streamingData), HttpStatusCode.OK);
        }

        var method = typeof(ObservableQueryHttp).GetMethod(nameof(CreateResponseForObservable), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var genericMethod = method.MakeGenericMethod(observableInterface.GetGenericArguments()[0]);
        var responseTask = (Task<ObservableQueryHttpResponse>)genericMethod.Invoke(null, [queryContext, streamingData, options, cancellationToken])!;

        return await responseTask;
    }

    static async Task<ObservableQueryHttpResponse> CreateResponseForObservable<T>(
        QueryContext queryContext,
        object streamingData,
        ObservableQueryHttpOptions options,
        CancellationToken cancellationToken)
    {
        if (TryGetCurrentValue(streamingData, out var currentValue))
        {
            return new(CreateSuccessResult(queryContext, currentValue), HttpStatusCode.OK);
        }

        if (!options.WaitForFirstResult)
        {
            return new(
                QueryResult.Error(
                    queryContext.CorrelationId,
                    $"Observable query has not produced its first result yet. Add '{WaitForFirstResultQueryStringKey}=true' to wait for the first observable query result."),
                HttpStatusCode.Accepted);
        }

        try
        {
            var observable = (IObservable<T>)streamingData;
            var waitResult = await WaitForFirstResult(observable, options, cancellationToken);

            if (waitResult.TimedOut)
            {
                return new(
                    QueryResult.Error(
                        queryContext.CorrelationId,
                        $"Timed out waiting {options.WaitForFirstResultTimeout.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture)} seconds for the first observable query result."),
                    HttpStatusCode.RequestTimeout);
            }

            if (waitResult.CompletedWithoutResult)
            {
                return new(
                    QueryResult.Error(queryContext.CorrelationId, "Observable query completed before producing its first result."),
                    HttpStatusCode.InternalServerError);
            }

            return new(CreateSuccessResult(queryContext, waitResult.Value), HttpStatusCode.OK);
        }
        catch (InvalidCastException exception)
        {
            return new(QueryResult.Error(queryContext.CorrelationId, exception), HttpStatusCode.InternalServerError);
        }
    }

    static async Task<ObservableWaitResult<T>> WaitForFirstResult<T>(
        IObservable<T> observable,
        ObservableQueryHttpOptions options,
        CancellationToken cancellationToken)
    {
        var taskCompletionSource = new TaskCompletionSource<ObservableWaitResult<T>>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            using var subscription = observable.Subscribe(System.Reactive.Observer.Create<T>(
                value => taskCompletionSource.TrySetResult(new(false, false, value)),
                exception => taskCompletionSource.TrySetException(exception),
                () => taskCompletionSource.TrySetResult(new(false, true, default!))));

            return await taskCompletionSource.Task.WaitAsync(options.WaitForFirstResultTimeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            return new(true, false, default!);
        }
    }

    static QueryResult CreateSuccessResult(QueryContext queryContext, object? data) =>
        new()
        {
            CorrelationId = queryContext.CorrelationId,
            Data = data!,
            IsAuthorized = true,
            ValidationResults = [],
            ExceptionMessages = [],
            ExceptionStackTrace = string.Empty,
            Paging = new(queryContext.Paging.Page, queryContext.Paging.Size, queryContext.TotalItems)
        };

    static Type? GetObservableInterfaceFor(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IObservable<>)
            ? type
            : type.GetInterfaces().FirstOrDefault(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IObservable<>));

    static bool TryGetCurrentValue(object streamingData, out object? currentValue)
    {
        var valueProperty = streamingData.GetType().GetProperty("Value");
        if (valueProperty is not null)
        {
            currentValue = valueProperty.GetValue(streamingData);
            return true;
        }

        currentValue = null;
        return false;
    }

    static bool TryGetValue(IReadOnlyDictionary<string, string> values, string key, out string value)
    {
        if (values.TryGetValue(key, out value!))
        {
            return true;
        }

        var matchedValue = values
            .Where(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))
            .Select(item => item.Value)
            .FirstOrDefault();

        if (matchedValue is not null)
        {
            value = matchedValue;
            return true;
        }

        value = string.Empty;
        return false;
    }
    readonly record struct ObservableWaitResult<T>(bool TimedOut, bool CompletedWithoutResult, T Value);
}
