// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { GetHttpHeaders } from './GetHttpHeaders';
import { QueryTransportMethod } from './queries/QueryTransportMethod';

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
}

export const Globals: IGlobals = {
    microservice: '',
    apiBasePath: '',
    origin: '',
    microserviceHttpHeader: 'x-cratis-microservice',
    microserviceWSQueryArgument: 'x-cratis-microservice',
    queryTransportMethod: QueryTransportMethod.WebSocket,
    queryConnectionCount: 1,
    queryDirectMode: false,
    observableQueryTransferMode: ObservableQueryTransferMode.Delta,
    httpHeadersCallback: () => ({}),
};