// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { GetHttpHeaders } from './GetHttpHeaders';
import { EventSourceFactory } from './EventSourceFactory';
import { QueryTransportMethod } from './queries/QueryTransportMethod';
import { QueryHttpMethod } from './queries/QueryHttpMethod';
import { QueryHttpMethodResolver } from './queries/QueryHttpMethodResolver';

/**
 * Defines the transfer mode used for observable query subscriptions.
 *
 * - {@link ObservableQueryTransferMode.Delta} (default): Only the items that changed since
 *   the previous update (added, replaced, removed) are exposed via {@code useChangeStream}.
 *   The full collection state is still maintained internally by {@code useObservableQuery}.
 *
 * - {@link ObservableQueryTransferMode.Full}: Every update delivers the complete current
 *   collection. The change set returned by {@code useChangeStream} treats the entire new
 *   collection as {@code added} and the previous collection as {@code removed}.
 */
export enum ObservableQueryTransferMode {
    /** Compute and expose only the items that changed since the previous update (default). */
    Delta = 'delta',
    /** Expose the full collection on every update. */
    Full = 'full',
}

export interface IGlobals {
    microservice: string;
    apiBasePath: string;
    origin: string;
    microserviceHttpHeader: string;
    microserviceWSQueryArgument: string;
    queryTransportMethod: QueryTransportMethod;
    /**
     * The HTTP method used to perform non-streaming queries. Defaults to {@link QueryHttpMethod.Get}.
     * Set to {@link QueryHttpMethod.Query} to carry arguments in a JSON request body (RFC 10008)
     * instead of the URL query string. Individual queries can override this via {@code setHttpMethod}.
     */
    queryHttpMethod: QueryHttpMethod;
    /**
     * Optional per-query policy for choosing the query HTTP method from the request (e.g. by URL length).
     * Consulted only when a query has no explicit method set via {@code setHttpMethod}; it takes
     * precedence over {@link queryHttpMethod}. See {@link lengthBasedQueryHttpMethod} for a built-in.
     */
    queryHttpMethodResolver?: QueryHttpMethodResolver;
    /**
     * Number of hub connections maintained for observable queries.
     * When greater than one, queries are distributed across the pool round-robin.
     * Only applies when {@link queryTransportMethod} is a centralized hub transport.
     * Defaults to 1.
     */
    queryConnectionCount: number;
    /**
     * When true, observable queries connect directly to the per-query WebSocket URL
     * instead of routing through the centralized hub endpoint.
     * Defaults to false (use the centralized hub).
     */
    queryDirectMode: boolean;
    /**
     * Controls how observable query updates are transferred and exposed.
     * {@link ObservableQueryTransferMode.Delta} (default) computes per-update deltas;
     * {@link ObservableQueryTransferMode.Full} delivers the whole collection on every update.
     */
    observableQueryTransferMode: ObservableQueryTransferMode;
    /**
     * Callback that returns custom HTTP headers to include in hub transport requests
     * (e.g. SSE subscribe/unsubscribe POST calls).
     */
    httpHeadersCallback: GetHttpHeaders;
    /**
     * Optional factory used to create the {@link EventSource} instances that back SSE
     * observable query connections. Falls back to the global {@link EventSource}
     * constructor when not set — override it to supply a custom SSE client (e.g. a
     * native implementation on React Native, where the global constructor is unavailable).
     */
    eventSourceFactory?: EventSourceFactory;
    /**
     * How long in milliseconds to retain a query cache entry after the last subscriber
     * releases it before evicting the subscription and the cached data.
     *
     * A non-zero value allows a user navigating away and back quickly to see cached
     * data immediately instead of waiting for a fresh server round-trip.  The entry is
     * still evicted once the window expires, so memory is eventually reclaimed.
     *
     * Defaults to 30 000 ms (30 seconds).  Set to 0 to restore the previous
     * immediate-eviction behaviour.
     */
    queryCacheRetentionMs: number;
}

export const Globals: IGlobals = {
    microservice: '',
    apiBasePath: '',
    origin: '',
    microserviceHttpHeader: 'x-cratis-microservice',
    microserviceWSQueryArgument: 'x-cratis-microservice',
    queryTransportMethod: QueryTransportMethod.WebSocket,
    queryHttpMethod: QueryHttpMethod.Get,
    queryConnectionCount: 1,
    queryDirectMode: false,
    observableQueryTransferMode: ObservableQueryTransferMode.Delta,
    httpHeadersCallback: () => ({}),
    queryCacheRetentionMs: 30_000,
};