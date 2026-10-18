// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Net;
using System.Threading.Channels;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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
        var elementType = ObservableQueryElementOperations.FindElementType(streamingData.GetType(), typeof(IObservable<>));
        if (elementType is null)
        {
            return new(CreateSuccessResult(queryContext, streamingData), HttpStatusCode.OK);
        }

        return await ObservableQueryElementOperations.For(elementType).CreateHttpResponse(new(queryContext, options, null, cancellationToken), streamingData);
    }

    /// <summary>
    /// Creates an HTTP response after request-scoped interception and emission guards.
    /// </summary>
    /// <param name="queryContext">The query context.</param>
    /// <param name="streamingData">The observable or async enumerable result.</param>
    /// <param name="options">The snapshot wait options.</param>
    /// <param name="requestContext">The current request.</param>
    /// <returns>The HTTP response to send.</returns>
    internal static async Task<ObservableQueryHttpResponse> CreateResponse(
        QueryContext queryContext,
        object streamingData,
        ObservableQueryHttpOptions options,
        IHttpRequestContext requestContext)
    {
        var elementType = ObservableQueryElementOperations.FindElementType(streamingData.GetType(), typeof(IObservable<>)) ??
            ObservableQueryElementOperations.FindElementType(streamingData.GetType(), typeof(IAsyncEnumerable<>));

        var response = elementType is null
            ? new ObservableQueryHttpResponse(CreateSuccessResult(queryContext, streamingData), HttpStatusCode.OK)
            : await ObservableQueryElementOperations.For(elementType).CreateHttpResponse(
                new(queryContext, options, requestContext, requestContext.RequestAborted), streamingData);
        var services = requestContext.RequestServices;
        var exposeExceptionDetails = services.GetService<IOptions<ArcOptions>>()?.Value.ExposeExceptionDetails ?? false;
        ExceptionDetailRedactor.Redact(response.Result, exposeExceptionDetails, GetLogger(services));

        return response;
    }

    /// <summary>
    /// Creates the HTTP response for an observable of a known element type.
    /// </summary>
    /// <typeparam name="T">The element type of the observable.</typeparam>
    /// <param name="context">The query and request state for the HTTP response.</param>
    /// <param name="streamingData">The observable query result.</param>
    /// <returns>The <see cref="ObservableQueryHttpResponse"/> to send.</returns>
    internal static async Task<ObservableQueryHttpResponse> CreateResponseForObservable<T>(ObservableQueryHttpContext context, object streamingData)
    {
        if (context.Request is not null)
        {
            return await CreateRequestResponse<T>(context, streamingData);
        }

        var queryContext = context.Query;
        var options = context.Options;
        if (TryGetCurrentValue(streamingData, out var currentValue))
        {
            return new(CreateSuccessResult(queryContext, currentValue), HttpStatusCode.OK);
        }

        if (!options.WaitForFirstResult)
        {
            // The observable has not produced its first result yet — a transient, expected state (for example a page
            // past the end of the data, or a subscription that has not emitted yet), not a failure. Return a not-ready
            // result (no exception) with 202 Accepted so a caller does not read a pending query as a crash; the caller
            // can add the wait-for-first-result query string key to wait for the first result instead.
            return new(QueryResult.NotReady(queryContext.CorrelationId), HttpStatusCode.Accepted);
        }

        try
        {
            var observable = (IObservable<T>)streamingData;
            var waitResult = await WaitForFirstResult(observable, options, context.CancellationToken);

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

    static async Task<ObservableQueryHttpResponse> CreateRequestResponse<T>(ObservableQueryHttpContext context, object streamingData)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(context.Request!.RequestAborted);
        ConfigureDeadline<T>(deadline, context.Options, streamingData);

        // Only this request's consumer invokes scoped collaborators. Producer callbacks merely enqueue values,
        // so their ambient identity cannot leak into interception and no asynchronous callback outlives the scope.
        var services = context.Request!.RequestServices;
        try
        {
            using var identity = BeginRequestEmissionIdentity(context, services);
            var processing = new EmissionProcessingContext(
                context,
                services.GetRequiredService<IReadModelInterceptors>(),
                services.GetRequiredService<IObservableQueryEmissionGuards>(),
                deadline.Token);
            return streamingData is IAsyncEnumerable<T> enumerable
                ? await CreateEnumerableResponse(processing, enumerable)
                : await CreateObservableResponse(processing, (IObservable<T>)streamingData);
        }
        catch (OperationCanceledException) when (context.Request.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            return new(
                QueryResult.Error(
                    context.Query.CorrelationId,
                    $"Timed out waiting {context.Options.WaitForFirstResultTimeout.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture)} seconds for the first observable query result."),
                HttpStatusCode.RequestTimeout);
        }
        catch (Exception error) when (!IsFatal(error))
        {
            GetLogger(services).ObservableHttpQueryFailed(error, context.Query.CorrelationId);
            return new(QueryResult.Error(context.Query.CorrelationId, error), HttpStatusCode.InternalServerError);
        }
    }

    static void ConfigureDeadline<T>(CancellationTokenSource deadline, ObservableQueryHttpOptions options, object streamingData)
    {
        if (options.WaitForFirstResult && streamingData is IObservable<T>)
        {
            deadline.CancelAfter(options.WaitForFirstResultTimeout);
        }
    }

    static IDisposable BeginRequestEmissionIdentity(ObservableQueryHttpContext context, IServiceProvider services)
    {
        var request = context.Request!;
        var tenant = context.Query.EmissionTenant ?? services.GetService<TenantIdAccessor>()?.Current;
        var nativeRequest = context.Query.NativeEmissionRequest ??
            (services.GetService<IAuthorizationPolicyRuntime>() as IAuthorizationEmissionRuntime)?.CaptureLiveRequest(services);

        return ObservableEmissionIdentity.Begin(request, services, context.Query.AuthorizedPrincipal ?? request.User, tenant, nativeRequest);
    }

    static async Task<ObservableQueryHttpResponse> CreateObservableResponse<T>(EmissionProcessingContext processing, IObservable<T> streamingData)
    {
        var queryContext = processing.Http.Query;
        if (!processing.Http.Options.WaitForFirstResult)
        {
            return TryGetCurrentValue(streamingData, out var currentValue)
                ? await ProcessEmission<T>(processing, currentValue)
                : new(QueryResult.NotReady(queryContext.CorrelationId), HttpStatusCode.Accepted);
        }

        var emissions = Channel.CreateUnbounded<T>(new UnboundedChannelOptions { SingleReader = true, AllowSynchronousContinuations = false });
        using var subscription = streamingData.Subscribe(System.Reactive.Observer.Create<T>(
            value => emissions.Writer.TryWrite(value),
            error => emissions.Writer.TryComplete(error),
            () => emissions.Writer.TryComplete()));

        var cachedResponse = await ProcessUnreplayedCache(processing, streamingData, emissions.Reader);
        if (cachedResponse is { StatusCode: not HttpStatusCode.Accepted } readyResponse)
        {
            return readyResponse;
        }

        await foreach (var value in emissions.Reader.ReadAllAsync(processing.Token))
        {
            var response = await ProcessEmission<T>(processing, value);
            if (response.StatusCode != HttpStatusCode.Accepted)
            {
                return response;
            }
        }

        return new(QueryResult.Error(queryContext.CorrelationId, "Observable query completed before producing its first result."), HttpStatusCode.InternalServerError);
    }

    static async Task<ObservableQueryHttpResponse?> ProcessUnreplayedCache<T>(
        EmissionProcessingContext processing,
        IObservable<T> streamingData,
        ChannelReader<T> emissions)
    {
        // Replay subjects supply their current value while subscribing. Other Value-bearing observables may
        // not replay (including a completed BehaviorSubject), so process their cache if no value was queued.
        if (!emissions.TryPeek(out _) && TryGetCurrentValue(streamingData, out var cached))
        {
            return await ProcessEmission<T>(processing, cached);
        }

        return null;
    }

    static async Task<ObservableQueryHttpResponse> ProcessEmission<T>(EmissionProcessingContext processing, object? value)
    {
        var queryContext = processing.Http.Query;
        var token = processing.Token;
        token.ThrowIfCancellationRequested();
        var data = await processing.Interceptors.InterceptEmission(typeof(T), value, processing.Http.Request!.RequestServices);
        token.ThrowIfCancellationRequested();
        var verdict = await Guard(processing);
        token.ThrowIfCancellationRequested();

        return verdict switch
        {
            ObservableQueryEmissionVerdict.Suppress => new(QueryResult.NotReady(queryContext.CorrelationId), HttpStatusCode.Accepted),
            ObservableQueryEmissionVerdict.DenyAndTerminate => new(QueryResult.Unauthorized(queryContext.CorrelationId), HttpStatusCode.Forbidden),
            _ => new(CreateSuccessResult(queryContext, data), HttpStatusCode.OK)
        };
    }

    static async Task<ObservableQueryHttpResponse> CreateEnumerableResponse<T>(EmissionProcessingContext processing, IAsyncEnumerable<T> enumerable)
    {
        var values = new List<object>();
        await foreach (var value in enumerable.WithCancellation(processing.Token))
        {
            var response = await ProcessEmission<T>(processing with { IsFirstEmission = values.Count == 0 }, value);
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return response;
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                values.Add(response.Result.Data);
            }
        }

        return new(CreateSuccessResult(processing.Http.Query, values), HttpStatusCode.OK);
    }

    static Task<ObservableQueryEmissionVerdict> Guard(EmissionProcessingContext processing)
    {
        var queryContext = processing.Http.Query;
        var request = processing.Http.Request!;

        return processing.Guards.HasGuards
            ? processing.Guards.Guard(new ObservableQueryEmissionContext(
                queryContext.Name,
                queryContext.Arguments ?? QueryArguments.Empty,
                queryContext.AuthorizedPrincipal ?? request.User,
                queryContext.CorrelationId,
                request.RequestServices,
                processing.IsFirstEmission,
                processing.Token)
            {
                SubscriptionScopeSnapshot = queryContext.SubscriptionScopeSnapshot
            })
            : Task.FromResult(ObservableQueryEmissionVerdict.Allow);
    }

    static ILogger GetLogger(IServiceProvider services) =>
        services.GetService<ILoggerFactory>()?.CreateLogger(typeof(ObservableQueryHttp).FullName!) ?? NullLogger.Instance;

    static bool IsFatal(Exception error) => error is OutOfMemoryException or StackOverflowException or
        AccessViolationException or AppDomainUnloadedException or BadImageFormatException or
        CannotUnloadAppDomainException or InvalidProgramException;

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
            Paging = ObservableQueryPaging.ReportsPaging(data?.GetType())
                ? new(queryContext.Paging.Page, queryContext.Paging.Size, queryContext.TotalItems)
                : PagingInfo.NotPaged
        };

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

    sealed record EmissionProcessingContext(
        ObservableQueryHttpContext Http,
        IReadModelInterceptors Interceptors,
        IObservableQueryEmissionGuards Guards,
        CancellationToken Token)
    {
        /// <summary>
        /// Gets whether this emission precedes the first delivered result.
        /// </summary>
        public bool IsFirstEmission { get; init; } = true;
    }
}
