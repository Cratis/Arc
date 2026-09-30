// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reactive.Subjects;
using Cratis.Arc.Http;
using Cratis.DependencyInjection;
using Cratis.Reflection;
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

    /// <summary>
    /// Determines if the current request should be handled as a Server-Sent Events (SSE) connection.
    /// </summary>
    /// <param name="context">The <see cref="IHttpRequestContext"/>.</param>
    /// <returns>True if the request should be handled as SSE, false otherwise.</returns>
    public bool ShouldHandleAsSSE(IHttpRequestContext context) =>
        context.Headers.TryGetValue("Accept", out var accept) &&
        accept.Contains(HttpRequestContextExtensions.SseContentType, StringComparison.OrdinalIgnoreCase);

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

    static ObservableQueryElementOperations OperationsFor(object streamingData, Type openGenericInterface) =>
        ObservableQueryElementOperations.For(ObservableQueryElementOperations.FindElementType(streamingData.GetType(), openGenericInterface)!);

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
        else if (ShouldHandleAsSSE(context))
        {
            logger.HandlingAsSSE();
            await HandleSubjectViaSSE(context, streamingData);
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
        else if (ShouldHandleAsSSE(context))
        {
            logger.HandlingAsSSE();
            await HandleAsyncEnumerableViaSSE(context, streamingData);
        }
        else
        {
            logger.HandlingAsHttp();
            await HandleAsyncEnumerableViaHttp(context, streamingData);
        }
    }

    async Task HandleSubjectViaWebSocket(IHttpRequestContext context, object streamingData)
    {
        var queryContext = queryContextManager.Current;
        var clientObservable = OperationsFor(streamingData, typeof(ISubject<>)).CreateClientObservable(serviceProvider, queryContext, streamingData);
        await clientObservable.HandleConnection(context);
    }

    async Task HandleSubjectViaSSE(IHttpRequestContext context, object streamingData)
    {
        var queryContext = queryContextManager.Current;
        var clientObservable = OperationsFor(streamingData, typeof(ISubject<>)).CreateClientObservableSSE(serviceProvider, queryContext, streamingData);
        await clientObservable.HandleConnection(context);
    }

#pragma warning disable IDE0060 // Remove unused parameter - kept for signature consistency
    async Task HandleSubjectViaHttp(IHttpRequestContext context, object streamingData)
#pragma warning restore IDE0060
    {
        var options = ObservableQueryHttp.GetOptions(context.Query);
        var response = await ObservableQueryHttp.CreateResponse(queryContextManager.Current, streamingData, options, context.RequestAborted);
        context.SetStatusCode((int)response.StatusCode);
        await context.WriteResponseAsJson(response.Result, typeof(QueryResult), context.RequestAborted);
    }

    async Task HandleAsyncEnumerableViaWebSocket(IHttpRequestContext context, object streamingData)
    {
        var queryContext = queryContextManager.Current;
        var clientEnumerableObservable = OperationsFor(streamingData, typeof(IAsyncEnumerable<>)).CreateClientEnumerableObservable(serviceProvider, queryContext, streamingData);
        await clientEnumerableObservable.HandleConnection(context);
    }

    async Task HandleAsyncEnumerableViaSSE(IHttpRequestContext context, object streamingData)
    {
        var queryContext = queryContextManager.Current;
        var clientEnumerableObservable = OperationsFor(streamingData, typeof(IAsyncEnumerable<>)).CreateClientEnumerableObservableSSE(serviceProvider, queryContext, streamingData);
        await clientEnumerableObservable.HandleConnection(context);
    }

#pragma warning disable IDE0060 // Remove unused parameter - kept for signature consistency
    async Task HandleAsyncEnumerableViaHttp(IHttpRequestContext context, object streamingData)
#pragma warning restore IDE0060
    {
        // For HTTP, we need to serialize the enumerable
        context.SetStatusCode(HttpStatusCode.BadRequest);
        await context.WriteResponseAsJson(new { message = "AsyncEnumerable queries require WebSocket connection" }, typeof(object), context.RequestAborted);
    }
}
