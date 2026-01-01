// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Arc.Http;
using Cratis.DependencyInjection;
using Cratis.Json;
using Cratis.Reflection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents an implementation of <see cref="IObservableQueryHandler"/> for base Arc.
/// </summary>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
[Singleton]
public class StreamingQueryHandler(ILogger<StreamingQueryHandler> logger) : IObservableQueryHandler
{
    readonly JsonSerializerOptions _jsonSerializerOptions = Globals.JsonSerializerOptions;

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
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var type = streamingData.GetType();
        var subjectType = type.GetInterfaces().First(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(ISubject<>));
        var elementType = subjectType.GetGenericArguments()[0];

        // Use reflection to subscribe to the observable
        var subscribeMethod = typeof(StreamingQueryHandler)
            .GetMethod(nameof(SubscribeToSubject), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(elementType);

        await (Task)subscribeMethod.Invoke(this, [streamingData, webSocket, context.RequestAborted])!;
    }

#pragma warning disable IDE0060 // Remove unused parameter - kept for signature consistency
    async Task HandleSubjectViaHttp(IHttpRequestContext context, object streamingData)
#pragma warning restore IDE0060
    {
        // For HTTP, serialize the current state from BehaviorSubject if available
        // This allows HTTP clients to get a snapshot of the observable's current value
        var type = streamingData.GetType();
        var subjectType = type.GetInterfaces().FirstOrDefault(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(ISubject<>));

        if (subjectType is not null)
        {
            var elementType = subjectType.GetGenericArguments()[0];

            // Check if it's a BehaviorSubject which has a Value property
            var valueProperty = type.GetProperty("Value");
            if (valueProperty is not null)
            {
                var currentValue = valueProperty.GetValue(streamingData);
                context.SetStatusCode(200);
                await context.WriteResponseAsJsonAsync(new { data = currentValue }, typeof(object), context.RequestAborted);
                return;
            }
        }

        // Fallback: observable queries without current state require WebSocket
        context.SetStatusCode(400);
        await context.WriteResponseAsJsonAsync(new { message = "Observable queries require WebSocket connection" }, typeof(object), context.RequestAborted);
    }

    async Task HandleAsyncEnumerableViaWebSocket(IHttpRequestContext context, object streamingData)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var type = streamingData.GetType();
        var enumerableType = type.GetInterfaces().First(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));
        var elementType = enumerableType.GetGenericArguments()[0];

        // Use reflection to enumerate
        var enumerateMethod = typeof(StreamingQueryHandler)
            .GetMethod(nameof(EnumerateAsyncEnumerable), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(elementType);

        await (Task)enumerateMethod.Invoke(this, [streamingData, webSocket, context.RequestAborted])!;
    }

#pragma warning disable IDE0060 // Remove unused parameter - kept for signature consistency
    async Task HandleAsyncEnumerableViaHttp(IHttpRequestContext context, object streamingData)
#pragma warning restore IDE0060
    {
        // For HTTP, we need to serialize the enumerable
        await context.WriteResponseAsJsonAsync(new { message = "AsyncEnumerable queries require WebSocket connection" }, typeof(object), context.RequestAborted);
        context.SetStatusCode(400);
    }

    async Task SubscribeToSubject<T>(ISubject<T> subject, IWebSocket webSocket, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource();

        using var subscription = subject.Subscribe(
            value =>
            {
                try
                {
                    var json = JsonSerializer.Serialize(value, _jsonSerializerOptions);
                    var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                    webSocket.SendAsync(new ArraySegment<byte>(bytes), System.Net.WebSockets.WebSocketMessageType.Text, true, cancellationToken).Wait(cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.ErrorSendingWebSocketMessage(ex);
                }
            },
            error =>
            {
                logger.ObservableError(error);
                tcs.TrySetResult();
            },
            () =>
            {
                logger.StreamingObservableCompleted();
                tcs.TrySetResult();
            });

        cancellationToken.Register(() => tcs.TrySetCanceled());

        await tcs.Task;
        await webSocket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Completed", CancellationToken.None);
    }

    async Task EnumerateAsyncEnumerable<T>(IAsyncEnumerable<T> enumerable, IWebSocket webSocket, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var item in enumerable.WithCancellation(cancellationToken))
            {
                var json = JsonSerializer.Serialize(item);
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                await webSocket.SendAsync(new ArraySegment<byte>(bytes), System.Net.WebSockets.WebSocketMessageType.Text, true, cancellationToken);
            }

            await webSocket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Completed", CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.ErrorEnumeratingAsyncEnumerable(ex);
            await webSocket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.InternalServerError, "Error", CancellationToken.None);
        }
    }
}
