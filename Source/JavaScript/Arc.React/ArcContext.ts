// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { GetHttpHeaders, Globals } from '@cratis/arc';
import { QueryTransportMethod } from '@cratis/arc/queries';
import React from 'react';

export interface ArcConfiguration {
    microservice: string;
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
}

export const ArcContext = React.createContext<ArcConfiguration>({
    microservice: Globals.microservice,
    development: false,
    origin: '',
    basePath: '',
    apiBasePath: '',
    httpHeadersCallback: () => ({}),
    queryTransportMethod: QueryTransportMethod.ServerSentEvents,
    queryConnectionCount: 1,
    queryDirectMode: false,
    queryVersion: 0,
    reconnectQueries: () => { /* no-op until Arc provider initializes */ },
});
