// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Globals } from '../Globals';
import { DataReceived } from './ObservableQueryConnection';
import { IReconnectPolicy } from './IReconnectPolicy';
import { ReconnectPolicy } from './ReconnectPolicy';
import { QueryResult } from './QueryResult';

/* eslint-disable @typescript-eslint/no-explicit-any */

/**
 * Message types matching the backend {@link ObservableQueryHubMessageType} enum.
 * Serialized as integers by the Cratis JSON configuration.
 */
export enum HubMessageType {
    Subscribe = 0,
    Unsubscribe = 1,
    QueryResult = 2,
    Unauthorized = 3,
    Error = 4,
    Ping = 5,
    Pong = 6,
    Connected = 7,
}

/**
 * Wire format for messages exchanged over the {@link WebSocketHubConnection}.
 */
export interface HubMessage {
    type: HubMessageType;
    queryId?: string;
    payload?: any;
    timestamp?: number;
}

/**
 * Matches the backend {@link ObservableQuerySubscriptionRequest} record.
 */
export interface SubscriptionRequest {
    queryName: string;
    arguments?: Record<string, string | null>;
    page?: number;
    pageSize?: number;
    sortBy?: string;
    sortDirection?: string;
}

interface ActiveSubscription {
    request: SubscriptionRequest;
    callback: DataReceived<any>;
}

/**
 * Represents a single multiplexed WebSocket connection to the observable query hub
 * at {@code /.cratis/queries/ws}.
 *
 * Multiple query subscriptions are carried over the same physical WebSocket. Each subscription
 * is identified by a client-generated {@code queryId}; the server tags every result message with
 * the same id so responses can be routed to the correct callback.
 */
export class WebSocketHubConnection {
    private _socket?: WebSocket;
    private _disconnected = false;
    private _subscriptions: Map<string, ActiveSubscription> = new Map();
    private _pingInterval?: ReturnType<typeof setInterval>;
    private readonly _pingIntervalMs: number = 10000;
    private _lastPingSentTime?: number;
    private _lastPongLatency: number = 0;
    private _latencySamples: number[] = [];

    /**
     * Initializes a new instance of {@link WebSocketHubConnection}.
     * @param {string} url The WebSocket URL of the hub endpoint (e.g. {@code ws://localhost:5000/.cratis/queries/ws}).
     * @param {string} microservice The microservice name to pass as a query argument.
     * @param {IReconnectPolicy} reconnectPolicy The reconnect policy to use (default: {@link ReconnectPolicy}).
     */
    constructor(private readonly _url: string, private readonly _microservice: string, private readonly _policy: IReconnectPolicy = new ReconnectPolicy()) {
    }

    /**
     * Gets the number of active query subscriptions on this connection.
     */
    get queryCount(): number {
        return this._subscriptions.size;
    }

    /**
     * Gets the latency of the last ping/pong sequence in milliseconds.
     */
    get lastPingLatency(): number {
        return this._lastPongLatency;
    }

    /**
     * Gets the rolling average latency in milliseconds.
     */
    get averageLatency(): number {
        if (this._latencySamples.length === 0) return 0;
        return this._latencySamples.reduce((a, b) => a + b, 0) / this._latencySamples.length;
    }

    /**
     * Subscribe to a query on this hub connection.
     * If the WebSocket is not yet open, the subscribe message will be sent once the connection is established.
     * @param {string} queryId Client-generated unique identifier for this subscription.
     * @param {SubscriptionRequest} request The subscription request payload.
     * @param {DataReceived<any>} callback Callback invoked whenever the server pushes a result for this query.
     */
    subscribe(queryId: string, request: SubscriptionRequest, callback: DataReceived<any>): void {
        this._subscriptions.set(queryId, { request, callback });
        this.ensureConnected();

        if (this._socket?.readyState === WebSocket.OPEN) {
            this.sendSubscribeMessage(queryId, request);
        }
        // If not yet open, sendAllSubscriptions will fire in onopen.
    }

    /**
     * Unsubscribe from a query on this hub connection.
     * @param {string} queryId The identifier of the subscription to cancel.
     */
    unsubscribe(queryId: string): void {
        this._subscriptions.delete(queryId);

        if (this._socket?.readyState === WebSocket.OPEN) {
            this.sendMessage({ type: HubMessageType.Unsubscribe, queryId });
        }

        // If no subscriptions remain, close the connection to free resources.
        if (this._subscriptions.size === 0) {
            this.close();
        }
    }

    /**
     * Permanently close this hub connection and clean up all subscriptions.
     */
    dispose(): void {
        this._disconnected = true;
        this._subscriptions.clear();
        this.stopPinging();
        this._policy.cancel();
        this._socket?.close();
        this._socket = undefined;
    }

    private ensureConnected(): void {
        if (this._disconnected) {
            // Reset disconnected flag when a new subscription comes in
            this._disconnected = false;
        }

        if (this._socket && (this._socket.readyState === WebSocket.OPEN || this._socket.readyState === WebSocket.CONNECTING)) {
            return;
        }

        this.openSocket();
    }

    private close(): void {
        this._disconnected = true;
        this.stopPinging();
        this._policy.cancel();
        this._socket?.close();
        this._socket = undefined;
    }

    private openSocket(): void {
        let url = this._url;
        if (this._microservice?.length > 0) {
            const param = `${Globals.microserviceWSQueryArgument}=${encodeURIComponent(this._microservice)}`;
            url += (url.includes('?') ? '&' : '?') + param;
        }

        this._socket = new WebSocket(url);

        this._socket.onopen = () => {
            if (this._disconnected) return;
            console.log(`Hub connection established: '${url}'`);
            this._policy.reset();
            this.startPinging();
            this.sendAllSubscriptions();
        };

        this._socket.onclose = () => {
            if (this._disconnected) return;
            console.log(`Hub connection closed: '${url}'`);
            this.stopPinging();
            if (this._subscriptions.size === 0) return;
            this._policy.schedule(() => {
                if (!this._disconnected && this._subscriptions.size > 0) {
                    this.openSocket();
                }
            }, this._url);
        };

        this._socket.onerror = (error) => {
            if (this._disconnected) return;
            console.error(`Hub connection error: '${url}'`, error);
            this.stopPinging();
            // onclose will fire after onerror, triggering reconnect
        };

        this._socket.onmessage = (ev) => {
            if (this._disconnected) return;
            this.handleMessage(ev.data as string);
        };
    }

    private sendAllSubscriptions(): void {
        for (const [queryId, sub] of this._subscriptions) {
            this.sendSubscribeMessage(queryId, sub.request);
        }
    }

    private sendSubscribeMessage(queryId: string, request: SubscriptionRequest): void {
        this.sendMessage({
            type: HubMessageType.Subscribe,
            queryId,
            payload: request,
        });
    }

    private sendMessage(message: HubMessage): void {
        if (this._socket?.readyState === WebSocket.OPEN) {
            this._socket.send(JSON.stringify(message));
        }
    }

    private handleMessage(rawData: string): void {
        try {
            const message = JSON.parse(rawData) as HubMessage;

            switch (message.type) {
                case HubMessageType.QueryResult:
                    this.handleQueryResult(message);
                    break;
                case HubMessageType.Pong:
                    this.handlePong(message);
                    break;
                case HubMessageType.Unauthorized:
                    console.warn(`Hub: query '${message.queryId}' unauthorized`);
                    break;
                case HubMessageType.Error:
                    console.error(`Hub: query '${message.queryId}' error:`, message.payload);
                    break;
            }
        } catch (error) {
            console.error('Hub: error parsing message', error);
        }
    }

    private handleQueryResult(message: HubMessage): void {
        if (!message.queryId) return;

        const sub = this._subscriptions.get(message.queryId);
        if (!sub) return;

        const result = message.payload as QueryResult<any>;
        sub.callback(result);
    }

    private handlePong(message: HubMessage): void {
        if (message.timestamp && this._lastPingSentTime) {
            const latency = Date.now() - message.timestamp;
            this._lastPongLatency = latency;
            this._latencySamples.push(latency);

            if (this._latencySamples.length > 100) {
                this._latencySamples.shift();
            }
        }
    }

    private startPinging(): void {
        this.stopPinging();
        this._pingInterval = setInterval(() => {
            if (this._socket?.readyState !== WebSocket.OPEN) return;
            this._lastPingSentTime = Date.now();
            this.sendMessage({
                type: HubMessageType.Ping,
                timestamp: this._lastPingSentTime,
            });
        }, this._pingIntervalMs);
    }

    private stopPinging(): void {
        if (this._pingInterval) {
            clearInterval(this._pingInterval);
            this._pingInterval = undefined;
        }
    }
}
