// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Globals } from '../Globals';
import { joinPaths } from '../joinPaths';
import { UrlHelpers } from '../UrlHelpers';
import { IObservableQueryConnection } from './IObservableQueryConnection';
import { ObservableQueryConnection } from './ObservableQueryConnection';
import { QueryTransportMethod } from './QueryTransportMethod';
import { ServerSentEventQueryConnection, SSE_HUB_ROUTE } from './ServerSentEventQueryConnection';
import { ServerSentEventHubConnection } from './ServerSentEventHubConnection';
import { WebSocketHubConnection } from './WebSocketHubConnection';
import { MultiplexedObservableQueryConnection, WS_HUB_ROUTE, getOrCreateMultiplexer } from './ObservableQueryMultiplexer';

/**
 * The SSE subscribe POST endpoint route.
 */
export const SSE_SUBSCRIBE_ROUTE = '/.cratis/queries/sse/subscribe';

/**
 * The SSE unsubscribe POST endpoint route.
 */
export const SSE_UNSUBSCRIBE_ROUTE = '/.cratis/queries/sse/unsubscribe';

/**
 * Describes what the factory needs to know about a query in order to create
 * the right connection. Transport details are intentionally absent — the
 * factory reads those from {@link Globals}.
 */
export interface QueryConnectionDescriptor {
    /** The route template (e.g. `/api/accounts/debit/all-accounts`). */
    route: string;
    /** Fully-qualified backend query name used by hub transports. */
    queryName: string;
    /** Origin base URL (e.g. `http://localhost:5000`). */
    origin: string;
    /** API base path prefix. */
    apiBasePath: string;
    /** Microservice identifier. */
    microservice: string;
    /** Route/query arguments for the current subscription. */
    args?: object;
}

/**
 * Single entry-point for creating observable query connections.
 *
 * The transport is chosen as a 2x2 matrix of (direct | hub) x (WebSocket | SSE):
 *
 * |                | Direct (per-query URL)         | Hub (centralized endpoint)    |
 * |----------------|-------------------------------|-------------------------------|
 * | **WebSocket**  | ObservableQueryConnection                | MultiplexedObservableQueryConnection |
 * | **SSE**        | ServerSentEventQueryConnection           | MultiplexedObservableQueryConnection |
 *
 * In multiplexed mode, both transports share the same multiplexer — it creates either
 * WebSocket or SSE connections depending on the transport setting.
 *
 * {@link ObservableQueryFor} never needs to know which transport is in use.
 */
export function createObservableQueryConnection<TDataType>(descriptor: QueryConnectionDescriptor): IObservableQueryConnection<TDataType> {
    const isSSE = Globals.queryTransportMethod === QueryTransportMethod.ServerSentEvents;

    if (Globals.queryDirectMode) {
        return isSSE
            ? createDirectSSEConnection<TDataType>(descriptor)
            : createDirectWebSocketConnection<TDataType>(descriptor);
    }

    return createMultiplexedConnection<TDataType>(descriptor, isSSE);
}

// ---- Direct mode: one connection per query, hitting the query's own URL ----

function buildDirectUrl(descriptor: QueryConnectionDescriptor): URL {
    const { route } = UrlHelpers.replaceRouteParameters(descriptor.route, descriptor.args as object);
    const actualRoute = joinPaths(descriptor.apiBasePath, route);
    return UrlHelpers.createUrlFrom(descriptor.origin, descriptor.apiBasePath, actualRoute);
}

function createDirectWebSocketConnection<TDataType>(descriptor: QueryConnectionDescriptor): IObservableQueryConnection<TDataType> {
    return new ObservableQueryConnection<TDataType>(buildDirectUrl(descriptor), descriptor.microservice);
}

function createDirectSSEConnection<TDataType>(descriptor: QueryConnectionDescriptor): IObservableQueryConnection<TDataType> {
    return new ServerSentEventQueryConnection<TDataType>(buildDirectUrl(descriptor));
}

// ---- Multiplexed mode: centralized endpoint, multiple queries share connections ----

function createMultiplexedConnection<TDataType>(descriptor: QueryConnectionDescriptor, isSSE: boolean): IObservableQueryConnection<TDataType> {
    const transport = isSSE ? 'sse' : 'ws';
    const cacheKey = `${Globals.queryConnectionCount}|${transport}|${descriptor.origin}|${descriptor.apiBasePath}|${descriptor.microservice}`;

    const multiplexer = getOrCreateMultiplexer(() => {
        if (isSSE) {
            const sseRoute = joinPaths(descriptor.apiBasePath, SSE_HUB_ROUTE);
            const sseUrl = UrlHelpers.createUrlFrom(descriptor.origin, descriptor.apiBasePath, sseRoute);
            const subscribeRoute = joinPaths(descriptor.apiBasePath, SSE_SUBSCRIBE_ROUTE);
            const subscribeUrl = UrlHelpers.createUrlFrom(descriptor.origin, descriptor.apiBasePath, subscribeRoute);
            const unsubscribeRoute = joinPaths(descriptor.apiBasePath, SSE_UNSUBSCRIBE_ROUTE);
            const unsubscribeUrl = UrlHelpers.createUrlFrom(descriptor.origin, descriptor.apiBasePath, unsubscribeRoute);
            return new ServerSentEventHubConnection(
                sseUrl.toString(),
                subscribeUrl.toString(),
                unsubscribeUrl.toString(),
                descriptor.microservice,
            );
        } else {
            const hubRoute = joinPaths(descriptor.apiBasePath, WS_HUB_ROUTE);
            const hubUrl = UrlHelpers.createUrlFrom(descriptor.origin, descriptor.apiBasePath, hubRoute);
            const secure = hubUrl.protocol?.indexOf('https') === 0;
            const wsUrl = `${secure ? 'wss' : 'ws'}://${hubUrl.host}${hubUrl.pathname}${hubUrl.search}`;
            return new WebSocketHubConnection(wsUrl, descriptor.microservice);
        }
    }, cacheKey);

    return new MultiplexedObservableQueryConnection<TDataType>(multiplexer, descriptor.queryName);
}
