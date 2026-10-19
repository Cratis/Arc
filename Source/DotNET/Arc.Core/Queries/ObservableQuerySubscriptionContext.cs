// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Execution;

namespace Cratis.Arc.Queries;

/// <summary>
/// Groups everything a demultiplexed connection supplies when it subscribes to a streaming query result, so the
/// subscription methods take the streamed data plus one context rather than a dozen individual arguments.
/// </summary>
/// <param name="Context">The subscribing connection's <see cref="IHttpRequestContext"/>.</param>
/// <param name="QueryId">The client's identifier for the subscription.</param>
/// <param name="Paging">The <see cref="PagingInfo"/> to report with each result.</param>
/// <param name="TransferMode">The requested transfer mode (for example delta or full), or null for the default. Not used by async enumerable streams.</param>
/// <param name="CorrelationId">The <see cref="CorrelationId"/> to stamp each result with.</param>
/// <param name="Identity">The <see cref="ObservableQuerySubscriptionIdentity"/> describing the query and the caller that established the subscription.</param>
/// <param name="OnNext">Called with each streamed <see cref="QueryResult"/> and the active subscription token.</param>
/// <param name="OnError">Called with the query id, message and active subscription token when the subscription errors.</param>
/// <param name="OnUnauthorized">Called with the query id and active subscription token when an emission guard denies the stream, ending the subscription.</param>
/// <param name="OnCompleted">Called to terminate the exact owning operation when the stream completes.</param>
/// <param name="AuthorizedQueryContext">The <see cref="QueryContext"/> the query was authorized with, if any. Not used by async enumerable streams.</param>
/// <param name="Token">The connection's <see cref="CancellationToken"/>.</param>
internal sealed record ObservableQuerySubscriptionContext(
    IHttpRequestContext Context,
    string QueryId,
    PagingInfo Paging,
    string? TransferMode,
    CorrelationId CorrelationId,
    ObservableQuerySubscriptionIdentity Identity,
    Func<QueryResult, CancellationToken, Task> OnNext,
    Func<string, string, CancellationToken, Task> OnError,
    Func<string, CancellationToken, Task> OnUnauthorized,
    Action OnCompleted,
    QueryContext? AuthorizedQueryContext,
    CancellationToken Token);
