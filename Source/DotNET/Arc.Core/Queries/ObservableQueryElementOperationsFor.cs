// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Http;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the <see cref="ObservableQueryElementOperations"/> for a specific element type.
/// </summary>
/// <typeparam name="TElement">The element type of the observable query result.</typeparam>
internal sealed class ObservableQueryElementOperationsFor<TElement> : ObservableQueryElementOperations
{
    /// <summary>
    /// The item type when <typeparamref name="TElement"/> is or implements <see cref="IEnumerable{T}"/>, found once
    /// per element type rather than on every emission that has no item to take it from.
    /// </summary>
    static readonly Type? _enumerableItemType = FindElementType(typeof(TElement), typeof(IEnumerable<>));

    /// <inheritdoc/>
    public override IClientObservable CreateClientObservable(IServiceProvider serviceProvider, QueryContext queryContext, object subject) =>
        ActivatorUtilities.CreateInstance<ClientObservable<TElement>>(serviceProvider, queryContext, subject);

    /// <inheritdoc/>
    public override IClientObservable CreateClientObservableSSE(IServiceProvider serviceProvider, QueryContext queryContext, object subject) =>
        ActivatorUtilities.CreateInstance<ClientObservableSSE<TElement>>(serviceProvider, queryContext, subject);

    /// <inheritdoc/>
    public override IClientEnumerableObservable CreateClientEnumerableObservable(IServiceProvider serviceProvider, QueryContext queryContext, object enumerable) =>
        ActivatorUtilities.CreateInstance<ClientEnumerableObservable<TElement>>(serviceProvider, queryContext, enumerable);

    /// <inheritdoc/>
    public override IClientEnumerableObservable CreateClientEnumerableObservableSSE(IServiceProvider serviceProvider, QueryContext queryContext, object enumerable) =>
        ActivatorUtilities.CreateInstance<ClientEnumerableObservableSSE<TElement>>(serviceProvider, queryContext, enumerable);

    /// <inheritdoc/>
    public override Task<ObservableQueryHttpResponse> CreateHttpResponse(
        QueryContext queryContext,
        object observable,
        ObservableQueryHttpOptions options,
        CancellationToken cancellationToken) =>
        ObservableQueryHttp.CreateResponseForObservable<TElement>(queryContext, observable, options, cancellationToken);

    /// <inheritdoc/>
    public override IDisposable SubscribeToSubject(
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
        CancellationToken token) =>
        demultiplexer.SubscribeToSubject(
            context,
            (ISubject<TElement>)subject,
            _enumerableItemType,
            queryId,
            paging,
            transferMode,
            correlationId,
            identity,
            onNext,
            onError,
            onUnauthorized,
            onCompleted,
            authorizedQueryContext,
            token);

    /// <inheritdoc/>
    public override Task StreamAsyncEnumerable(
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
        CancellationToken token) =>
        demultiplexer.StreamAsyncEnumerable(
            context,
            (IAsyncEnumerable<TElement>)enumerable,
            queryId,
            paging,
            correlationId,
            identity,
            guardServiceProvider,
            onNext,
            onError,
            onUnauthorized,
            token);
}
