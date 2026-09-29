// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Reflection;
using Cratis.Arc.Http;
using Cratis.Execution;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the operations on an observable query result that depend on the result's element type.
/// </summary>
/// <remarks>
/// An observable query returns its <see cref="ISubject{T}"/>, <see cref="IObservable{T}"/> or
/// <see cref="IAsyncEnumerable{T}"/> as an <see cref="object"/>, so its element type is only known at runtime. Rather
/// than building a generic method call through reflection at every site that handles such a result, the operations
/// are closed over the element type once, here, and cached per element type. Everything past this point is statically
/// typed, which keeps trimming and NativeAOT analysis to this one place.
/// </remarks>
internal abstract class ObservableQueryElementOperations
{
    static readonly ConcurrentDictionary<Type, ObservableQueryElementOperations> _operationsByElementType = new();

    /// <summary>
    /// Gets the operations for an element type.
    /// </summary>
    /// <param name="elementType">The element type of the observable query result.</param>
    /// <returns>The <see cref="ObservableQueryElementOperations"/> for the element type.</returns>
    public static ObservableQueryElementOperations For(Type elementType) =>
        _operationsByElementType.GetOrAdd(
            elementType,
            static type => (ObservableQueryElementOperations)typeof(ObservableQueryElementOperations)
                .GetMethod(nameof(CreateOperations), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(type)
                .Invoke(null, null)!);

    /// <summary>
    /// Finds the type argument of a single-parameter open generic interface that a type is or implements.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="openGenericInterface">The open generic interface, for instance <see cref="IObservable{T}"/>.</param>
    /// <returns>The type argument of the first matching interface, or <see langword="null"/> if there is none.</returns>
    public static Type? FindElementType(Type type, Type openGenericInterface) =>
        (type.IsGenericType && type.GetGenericTypeDefinition() == openGenericInterface
            ? type
            : type.GetInterfaces().FirstOrDefault(_ => _.IsGenericType && _.GetGenericTypeDefinition() == openGenericInterface))?.GetGenericArguments()[0];

    /// <summary>
    /// Creates the WebSocket client observable for a subject.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> resolving the observable's dependencies.</param>
    /// <param name="queryContext">The <see cref="QueryContext"/> of the query.</param>
    /// <param name="subject">The subject.</param>
    /// <returns>The <see cref="IClientObservable"/>.</returns>
    public abstract IClientObservable CreateClientObservable(IServiceProvider serviceProvider, QueryContext queryContext, object subject);

    /// <summary>
    /// Creates the Server-Sent Events client observable for a subject.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> resolving the observable's dependencies.</param>
    /// <param name="queryContext">The <see cref="QueryContext"/> of the query.</param>
    /// <param name="subject">The subject.</param>
    /// <returns>The <see cref="IClientObservable"/>.</returns>
    public abstract IClientObservable CreateClientObservableSSE(IServiceProvider serviceProvider, QueryContext queryContext, object subject);

    /// <summary>
    /// Creates the WebSocket client observable for an async enumerable.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> resolving the observable's dependencies.</param>
    /// <param name="queryContext">The <see cref="QueryContext"/> of the query.</param>
    /// <param name="enumerable">The async enumerable.</param>
    /// <returns>The <see cref="IClientEnumerableObservable"/>.</returns>
    public abstract IClientEnumerableObservable CreateClientEnumerableObservable(IServiceProvider serviceProvider, QueryContext queryContext, object enumerable);

    /// <summary>
    /// Creates the Server-Sent Events client observable for an async enumerable.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> resolving the observable's dependencies.</param>
    /// <param name="queryContext">The <see cref="QueryContext"/> of the query.</param>
    /// <param name="enumerable">The async enumerable.</param>
    /// <returns>The <see cref="IClientEnumerableObservable"/>.</returns>
    public abstract IClientEnumerableObservable CreateClientEnumerableObservableSSE(IServiceProvider serviceProvider, QueryContext queryContext, object enumerable);

    /// <summary>
    /// Creates the HTTP response for an observable.
    /// </summary>
    /// <param name="queryContext">The <see cref="QueryContext"/> for the current request.</param>
    /// <param name="observable">The observable.</param>
    /// <param name="options">The <see cref="ObservableQueryHttpOptions"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> for the request.</param>
    /// <returns>The <see cref="ObservableQueryHttpResponse"/> to send.</returns>
    public abstract Task<ObservableQueryHttpResponse> CreateHttpResponse(
        QueryContext queryContext,
        object observable,
        ObservableQueryHttpOptions options,
        CancellationToken cancellationToken);

    /// <summary>
    /// Subscribes a demultiplexed connection to a subject.
    /// </summary>
    /// <param name="demultiplexer">The <see cref="ObservableQueryDemultiplexer"/> owning the subscription.</param>
    /// <param name="context">The <see cref="IHttpRequestContext"/> of the connection.</param>
    /// <param name="subject">The subject.</param>
    /// <param name="queryId">The client's identifier for the subscription.</param>
    /// <param name="paging">The <see cref="PagingInfo"/> of the query.</param>
    /// <param name="transferMode">The requested transfer mode.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> of the query.</param>
    /// <param name="identity">The <see cref="ObservableQuerySubscriptionIdentity"/>.</param>
    /// <param name="onNext">Called for each result.</param>
    /// <param name="onError">Called when the subscription fails.</param>
    /// <param name="onUnauthorized">Called when the subscription is no longer authorized.</param>
    /// <param name="onCompleted">Called when the subject completes.</param>
    /// <param name="authorizedQueryContext">The <see cref="QueryContext"/> the query was authorized with, if any.</param>
    /// <param name="token">The <see cref="CancellationToken"/> ending the subscription.</param>
    /// <returns>The subscription.</returns>
    public abstract IDisposable SubscribeToSubject(
        ObservableQueryDemultiplexer demultiplexer,
        IHttpRequestContext context,
        object subject,
        string queryId,
        PagingInfo paging,
        string? transferMode,
        CorrelationId correlationId,
        ObservableQuerySubscriptionIdentity identity,
        Func<QueryResult, CancellationToken, Task> onNext,
        Func<string, string, CancellationToken, Task> onError,
        Func<string, CancellationToken, Task> onUnauthorized,
        Action onCompleted,
        QueryContext? authorizedQueryContext,
        CancellationToken token);

    /// <summary>
    /// Streams an async enumerable to a demultiplexed connection.
    /// </summary>
    /// <param name="demultiplexer">The <see cref="ObservableQueryDemultiplexer"/> owning the stream.</param>
    /// <param name="context">The <see cref="IHttpRequestContext"/> of the connection.</param>
    /// <param name="enumerable">The async enumerable.</param>
    /// <param name="queryId">The client's identifier for the subscription.</param>
    /// <param name="paging">The <see cref="PagingInfo"/> of the query.</param>
    /// <param name="correlationId">The <see cref="CorrelationId"/> of the query.</param>
    /// <param name="identity">The <see cref="ObservableQuerySubscriptionIdentity"/>.</param>
    /// <param name="guardServiceProvider">The <see cref="IServiceProvider"/> for emission guards, if any are registered.</param>
    /// <param name="onNext">Called for each result.</param>
    /// <param name="onError">Called when the stream fails.</param>
    /// <param name="onUnauthorized">Called when the stream is no longer authorized.</param>
    /// <param name="token">The <see cref="CancellationToken"/> ending the stream.</param>
    /// <returns>Awaitable task.</returns>
    public abstract Task StreamAsyncEnumerable(
        ObservableQueryDemultiplexer demultiplexer,
        IHttpRequestContext context,
        object enumerable,
        string queryId,
        PagingInfo paging,
        CorrelationId correlationId,
        ObservableQuerySubscriptionIdentity identity,
        IServiceProvider? guardServiceProvider,
        Func<QueryResult, CancellationToken, Task> onNext,
        Func<string, string, CancellationToken, Task> onError,
        Func<string, CancellationToken, Task> onUnauthorized,
        CancellationToken token);

    static ObservableQueryElementOperationsFor<TElement> CreateOperations<TElement>() => new();
}
