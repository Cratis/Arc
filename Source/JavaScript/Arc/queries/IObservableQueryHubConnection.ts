// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DataReceived } from './ObservableQueryConnection';
import { SubscriptionRequest } from './WebSocketHubConnection';

/* eslint-disable @typescript-eslint/no-explicit-any */

/**
 * Defines a multiplexed hub connection that carries multiple observable query subscriptions
 * over a single transport (WebSocket or SSE + POST).
 *
 * Both {@link WebSocketHubConnection} and {@link ServerSentEventHubConnection} implement this
 * interface, allowing the pool to work with either transport interchangeably.
 */
export interface IObservableQueryHubConnection {
    /**
     * Gets the number of active query subscriptions on this connection.
     */
    readonly queryCount: number;

    /**
     * Gets the latency of the last ping/pong sequence in milliseconds.
     */
    readonly lastPingLatency: number;

    /**
     * Gets the rolling average latency in milliseconds.
     */
    readonly averageLatency: number;

    /**
     * Subscribe to a query on this hub connection.
     * @param {string} queryId Client-generated unique identifier for this subscription.
     * @param {SubscriptionRequest} request The subscription request payload.
     * @param {DataReceived<any>} callback Callback invoked whenever the server pushes a result for this query.
     */
    subscribe(queryId: string, request: SubscriptionRequest, callback: DataReceived<any>): void;

    /**
     * Unsubscribe from a query on this hub connection.
     * @param {string} queryId The identifier of the subscription to cancel.
     */
    unsubscribe(queryId: string): void;

    /**
     * Permanently close this hub connection and clean up all subscriptions.
     */
    dispose(): void;
}
