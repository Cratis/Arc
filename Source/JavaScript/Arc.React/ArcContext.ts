// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { GetHttpHeaders, ObservableQueryTransferMode, Globals } from '@cratis/arc';
import { QueryTransportMethod } from '@cratis/arc/queries';
import type { IObservableQueryDiagnostics } from '@cratis/arc/queries';
import { IMessenger, Messenger } from '@cratis/arc/messaging';
import React from 'react';

export interface ArcConfiguration {
    microservice: string;
    messenger?: IMessenger;
    development?: boolean
    origin?: string;
    basePath?: string;
    apiBasePath?: string;
    httpHeadersCallback?: GetHttpHeaders;
    queryTransportMethod?: QueryTransportMethod;
    /**
     * Number of hub connections maintained for observable queries.
     * When greater than one, queries are distributed across the pool round-robin.
     * Only applies when {@link queryTransportMethod} is a centralized hub transport.
     * Defaults to 1.
     */
    queryConnectionCount?: number;
    /**
     * When true, observable queries connect directly to the per-query WebSocket URL
     * instead of routing through the centralized hub endpoint.
     * Defaults to false (use the centralized hub).
     */
    queryDirectMode?: boolean;
    /**
     * Controls how observable query updates are transferred and exposed.
     * {@link ObservableQueryTransferMode.Delta} (default) computes per-update deltas;
     * {@link ObservableQueryTransferMode.Full} delivers the whole collection on every update.
     */
    observableQueryTransferMode?: ObservableQueryTransferMode;
    /**
     * How long in milliseconds to retain a query cache entry after the last subscriber
     * releases it.  Mirrors {@link Globals.queryCacheRetentionMs}.  Defaults to 30 000 ms.
     */
    queryCacheRetentionMs?: number;
    /**
     * Monotonically increasing version counter that is bumped by {@link reconnectQueries}
     * so that query hook effects re-run and re-establish subscriptions through fresh
     * transport connections. Do not set this directly.
     */
    queryVersion?: number;
    /**
     * Tears down all active query subscriptions, disposes the shared multiplexer, and
     * forces every observable query hook to re-subscribe through a fresh connection.
     *
     * Call this after an authentication state change (e.g. login or logout) so that
     * new WebSocket or SSE connections are established with the updated credentials
     * (cookies, headers).
     */
    reconnectQueries?: () => void;
    /**
     * Diagnostics service that exposes live snapshots of observable query state.
     * Consumers can call {@link IObservableQueryDiagnostics.getSnapshot} to retrieve
     * a point-in-time view, or {@link IObservableQueryDiagnostics.subscribe} to be
     * notified whenever a new snapshot is produced.
     */
    observableQueryDiagnostics?: IObservableQueryDiagnostics;
}

export const ArcContext = React.createContext<ArcConfiguration>({
    microservice: Globals.microservice,
    messenger: new Messenger(),
    development: false,
    origin: '',
    basePath: '',
    apiBasePath: '',
    httpHeadersCallback: () => ({}),
    queryTransportMethod: QueryTransportMethod.ServerSentEvents,
    queryConnectionCount: 1,
    queryDirectMode: false,
    observableQueryTransferMode: ObservableQueryTransferMode.Delta,
    queryCacheRetentionMs: 30_000,
    queryVersion: 0,
    reconnectQueries: () => { /* no-op until Arc provider initializes */ },
});
