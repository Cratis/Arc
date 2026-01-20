// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Http;
using Cratis.DependencyInjection;
using Cratis.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents a generic implementation of <see cref="IObservableQueryHandler"/> that doesn't depend on specific HTTP frameworks.
/// </summary>
/// <param name="queryContextManager"><see cref="IQueryContextManager"/>.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/>.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
[Singleton]
public class ObservableQueryHandler(
    IQueryContextManager queryContextManager,
    IServiceProvider serviceProvider,
    ILogger<ObservableQueryHandler> logger) : IObservableQueryHandler
{
    /// <inheritdoc/>
    public bool ShouldHandleAsWebSocket(IHttpRequestContext context) =>
        context.WebSockets.IsWebSocketRequest;

    /// <inheritdoc/>
    public bool IsStreamingResult(object? data) =>
        data?.GetType().ImplementsOpenGeneric(typeof(ISubject<>)) is true ||
        data?.GetType().ImplementsOpenGeneric(typeof(IAsyncEnumerable<>)) is true;

    /// <inheritdoc/>
    public async Task HandleStreamingResult(
        IHttpRequestContext context,
        QueryName queryName,
        object streamingData)
    {
        if (IsSubjectResult(streamingData))
        {
            await HandleSubjectResult(context, queryName, streamingData);
        }
        else if (IsAsyncEnumerableResult(streamingData))
        {
            await HandleAsyncEnumerableResult(context, queryName, streamingData);
        }
    }

    bool IsSubjectResult(object data) =>
        data.GetType().ImplementsOpenGeneric(typeof(ISubject<>));

    bool IsAsyncEnumerableResult(object data) =>
        data.GetType().ImplementsOpenGeneric(typeof(IAsyncEnumerable<>));

    async Task HandleSubjectResult(
        IHttpRequestContext context,
        QueryName queryName,
        object streamingData)
    {
        logger.ObservableQueryResult(queryName);

        if (context.WebSockets.IsWebSocketRequest)
        {
            logger.HandlingAsWebSocket();
            await HandleSubjectViaWebSocket(context, streamingData);
        }
        else
        {
            logger.HandlingAsHttp();
            await HandleSubjectViaHttp(context, streamingData);
        }
    }

    async Task HandleAsyncEnumerableResult(
        IHttpRequestContext context,
        QueryName queryName,
        object streamingData)
    {
        logger.AsyncEnumerableQueryResult(queryName);

        if (context.WebSockets.IsWebSocketRequest)
        {
            logger.HandlingAsWebSocket();
            await HandleAsyncEnumerableViaWebSocket(context, streamingData);
        }
        else
        {
            logger.HandlingAsHttp();
            await HandleAsyncEnumerableViaHttp(context, streamingData);
        }
    }

    async Task HandleSubjectViaWebSocket(IHttpRequestContext context, object streamingData)
    {
        var type = streamingData.GetType();
        var subjectType = type.GetInterfaces().First(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(ISubject<>));
        var elementType = subjectType.GetGenericArguments()[0];

        // Get the current query context
        var queryContext = queryContextManager.Current;

        // Create ClientObservable using ActivatorUtilities to get proper dependency injection
        var clientObservableType = typeof(ClientObservable<>).MakeGenericType(elementType);
        var clientObservable = ActivatorUtilities.CreateInstance(
            serviceProvider,
            clientObservableType,
            queryContext,
            streamingData) as IClientObservable;

        await clientObservable!.HandleConnection(context);
    }

#pragma warning disable IDE0060 // Remove unused parameter - kept for signature consistency
    async Task HandleSubjectViaHttp(IHttpRequestContext context, object streamingData)
#pragma warning restore IDE0060
    {
        // For HTTP, serialize the current state from BehaviorSubject if available
        // This allows HTTP clients to get a snapshot of the observable's current value
        var queryResult = await GetCurrentValueAsQueryResult(streamingData);
        context.SetStatusCode(200);
        await context.WriteResponseAsJsonAsync(queryResult, typeof(QueryResult), context.RequestAborted);
    }

    async Task HandleAsyncEnumerableViaWebSocket(IHttpRequestContext context, object streamingData)
    {
        var type = streamingData.GetType();
        var enumerableType = type.GetInterfaces().First(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));
        var elementType = enumerableType.GetGenericArguments()[0];

        // Get the current query context
        var queryContext = queryContextManager.Current;

        // Create ClientEnumerableObservable using ActivatorUtilities to get proper dependency injection
        var clientEnumerableObservableType = typeof(ClientEnumerableObservable<>).MakeGenericType(elementType);
        var clientEnumerableObservable = ActivatorUtilities.CreateInstance(
            serviceProvider,
            clientEnumerableObservableType,
            queryContext,
            streamingData) as IClientObservable;

        await clientEnumerableObservable!.HandleConnection(context);
    }

#pragma warning disable IDE0060 // Remove unused parameter - kept for signature consistency
    async Task HandleAsyncEnumerableViaHttp(IHttpRequestContext context, object streamingData)
#pragma warning restore IDE0060
    {
        // For HTTP, we need to serialize the enumerable
        await context.WriteResponseAsJsonAsync(new { message = "AsyncEnumerable queries require WebSocket connection" }, typeof(object), context.RequestAborted);
        context.SetStatusCode(400);
    }

    async Task<QueryResult> GetCurrentValueAsQueryResult(object streamingData)
    {
        var type = streamingData.GetType();
        var subjectType = type.GetInterfaces().FirstOrDefault(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(ISubject<>)) ??
                          type.GetInterfaces().FirstOrDefault(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IObservable<>));

        if (subjectType is null)
        {
            return new QueryResult { Data = streamingData };
        }

        // For Subjects/Observables, get the first emitted value using System.Reactive
        var dataType = subjectType.GetGenericArguments()[0];

        // Call Observable.FirstAsync<T>(observable).ToTask() using reflection
        var firstAsyncMethod = typeof(System.Reactive.Linq.Observable).GetMethods()
            .FirstOrDefault(m => m.Name == "FirstAsync" &&
                                  m.GetParameters().Length == 1 &&
                                  m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IObservable<>));

        if (firstAsyncMethod is null)
        {
            return new QueryResult { Data = null! };
        }

        var genericFirstAsync = firstAsyncMethod.MakeGenericMethod(dataType);
        var observableResult = genericFirstAsync.Invoke(null, [streamingData]);

        if (observableResult is null)
        {
            return new QueryResult { Data = null! };
        }

        // Convert to Task using ToTask()
        var toTaskMethod = typeof(System.Reactive.Threading.Tasks.TaskObservableExtensions).GetMethods()
            .FirstOrDefault(m => m.Name == "ToTask" &&
                                 m.GetParameters().Length == 1 &&
                                 m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IObservable<>));

        if (toTaskMethod is null)
        {
            return new QueryResult { Data = null! };
        }

        var genericToTask = toTaskMethod.MakeGenericMethod(dataType);
        var task = genericToTask.Invoke(null, [observableResult]);

        if (task is null)
        {
            return new QueryResult { Data = null! };
        }

        await (Task)task;
        var currentValue = ((dynamic)task).Result;

        return new QueryResult
        {
            Data = currentValue!,
            IsAuthorized = true,
            ValidationResults = [],
            ExceptionMessages = [],
            ExceptionStackTrace = string.Empty,
            Paging = new(queryContextManager.Current.Paging.Page, queryContextManager.Current.Paging.Size, queryContextManager.Current.TotalItems)
        };
    }
}
