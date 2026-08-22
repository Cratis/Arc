// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Globals } from '../Globals';
import type { DataReceived } from './ObservableQueryConnection';
import { HubConnectionKeepAlive } from './HubConnectionKeepAlive';
import type { IReconnectPolicy } from './IReconnectPolicy';
import { ReconnectPolicy } from './ReconnectPolicy';
import { QueryResult } from './QueryResult';

/* eslint-disable @typescript-eslint/no-explicit-any */

/**
 * Message types matching the backend {@link ObservableQueryHubMessageType} enum.
 * Serialized as strings by the JsonStringEnumConverter on the server.
 */
export enum HubMessageType {
    Subscribe = 'Subscribe',
    Unsubscribe = 'Unsubscribe',
    QueryResult = 'QueryResult',
    Unauthorized = 'Unauthorized',
    Error = 'Error',
    Ping = 'Ping',
    Pong = 'Pong',
    Connected = 'Connected',
}

/**
 * Wire format for messages exchanged over the observable query WebSocket and SSE hub transports.
 */
export interface HubMessage {
    type: HubMessageType;
    queryId?: string;

    /** Optional positive safe-integer revision used to reject stale replacement messages. */
    revision?: number;

    /**
     * Whether the server supports revision-aware subscription operations and result frames.
     * Only present on {@link HubMessageType.Connected} messages from capable servers.
     */
    supportsSubscriptionRevisions?: boolean;
    payload?: any;
    timestamp?: number;

    /**
     * The server's keep-alive interval in milliseconds, or 0 when keep-alive is disabled.
     * Only present on {@link HubMessageType.Connected} messages.
     */
    keepAliveIntervalMs?: number;
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
    transferMode?: string;
}

interface ActiveSubscription {
    request: SubscriptionRequest;
    callback: DataReceived<any>;
    revision: number;
}

/**
 * Represents a single multiplexed WebSocket connection to the observable query hub
 * at {@code /.cratis/queries/ws}.
 *
 * Multiple query subscriptions are carried over the same physical WebSocket. Each subscription
 * is identified by a client-generated {@code queryId}; the server tags every result message with
 * the same id so responses can be routed to the correct callback. The client initially subscribes
 * without revisions for compatibility with older servers, then replaces active subscriptions with
 * revision-aware operations when the server advertises support in a Connected message.
 */
export class WebSocketHubConnection {
    private _socket?: WebSocket;
    private _disconnected = false;
    private _subscriptions: Map<string, ActiveSubscription> = new Map();
    private readonly _keepAlive: HubConnectionKeepAlive;
    private _lastPingSentTime?: number;
    private _lastPongLatency: number = 0;
    private _latencySamples: number[] = [];
    private _nextRevision = 0;
    private _supportsSubscriptionRevisions = false;

    /**
     * Initializes a new instance of {@link WebSocketHubConnection}.
     * @param {string} _url The WebSocket URL of the hub endpoint (e.g. {@code ws://localhost:5000/.cratis/queries/ws}).
     * @param {string} _microservice The microservice name to pass as a query argument.
     * @param {number} pingIntervalMs How often to send keep-alive pings when the connection is idle (default: 10 000 ms).
     * @param {IReconnectPolicy} _policy The reconnect policy to use (default: {@link ReconnectPolicy}).
     */
    constructor(
        private readonly _url: string,
        private readonly _microservice: string,
        pingIntervalMs: number = 10000,
        private readonly _policy: IReconnectPolicy = new ReconnectPolicy(),
    ) {
        this._keepAlive = new HubConnectionKeepAlive(pingIntervalMs, () => {
            if (this._socket?.readyState === WebSocket.OPEN) {
                this._lastPingSentTime = Date.now();
                this.sendMessage({
                    type: HubMessageType.Ping,
                    timestamp: this._lastPingSentTime,
                });
            }
        });
    }

    /**
     * Gets the number of active query subscriptions on this connection.
     */
    get queryCount(): number {
        return this._subscriptions.size;
    }

    /**
     * Gets whether the WebSocket connection is currently open.
     */
    get isConnected(): boolean {
        return this._socket?.readyState === WebSocket.OPEN;
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
        return (
            this._latencySamples.reduce((a, b) => a + b, 0) / this._latencySamples.length
        );
    }

    /**
     * Subscribe to a query on this hub connection.
     * If the WebSocket is not yet open, the subscribe message will be sent once the connection is established.
     * @param {string} queryId Client-generated unique identifier for this subscription.
     * @param {SubscriptionRequest} request The subscription request payload.
     * @param {DataReceived<any>} callback Callback invoked whenever the server pushes a result for this query.
     */
    subscribe(
        queryId: string,
        request: SubscriptionRequest,
        callback: DataReceived<any>,
    ): void {
        const subscription = { request, callback, revision: this.getNextRevision() };
        this._subscriptions.set(queryId, subscription);
        this.ensureConnected();

        if (this._socket?.readyState === WebSocket.OPEN) {
            this.sendSubscribeMessage(queryId, subscription);
        }
        // If not yet open, sendAllSubscriptions will fire in onopen.
    }

    /**
     * Unsubscribe from a query on this hub connection.
     * @param {string} queryId The identifier of the subscription to cancel.
     */
    unsubscribe(queryId: string): void {
        const subscription = this._subscriptions.get(queryId);
        this._subscriptions.delete(queryId);

        if (this._socket?.readyState === WebSocket.OPEN && subscription) {
            const message: HubMessage = {
                type: HubMessageType.Unsubscribe,
                queryId,
            };
            if (this._supportsSubscriptionRevisions) {
                message.revision = subscription.revision;
            }
            this.sendMessage(message);
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
        this._keepAlive.stop();
        this._policy.cancel();
        this._socket?.close();
        this._socket = undefined;
    }

    private ensureConnected(): void {
        if (this._disconnected) {
            // Reset disconnected flag when a new subscription comes in
            this._disconnected = false;
        }

        if (
            this._socket &&
            (this._socket.readyState === WebSocket.OPEN ||
                this._socket.readyState === WebSocket.CONNECTING)
        ) {
            return;
        }

        this.openSocket();
    }

    private close(): void {
        this._disconnected = true;
        this._keepAlive.stop();
        this._policy.cancel();
        if (this._socket) {
            // Detach all handlers BEFORE closing so that the async onclose event cannot
            // fire after a new subscription has reset _disconnected to false and opened a
            // fresh socket. Without this, the stale onclose triggers an unintended
            // reconnect via the back-off policy, causing a 1-10 second delay before the
            // new page's queries receive their first data.
            this._socket.onopen = null;
            this._socket.onclose = null;
            this._socket.onerror = null;
            this._socket.onmessage = null;
            this._socket.close();
        }
        this._socket = undefined;
    }

    private openSocket(): void {
        this._supportsSubscriptionRevisions = false;

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
            this._keepAlive.start();
            this.sendAllSubscriptions();
        };

        this._socket.onclose = () => {
            if (this._disconnected) return;
            console.log(`Hub connection closed: '${url}'`);
            this._keepAlive.stop();
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
            this._keepAlive.stop();
            // onclose will fire after onerror, triggering reconnect
        };

        this._socket.onmessage = (ev) => {
            if (this._disconnected) return;
            this.handleMessage(ev.data as string);
        };
    }

    private sendAllSubscriptions(): void {
        for (const [queryId, sub] of this._subscriptions) {
            this.sendSubscribeMessage(queryId, sub);
        }
    }

    private sendSubscribeMessage(
        queryId: string,
        subscription: ActiveSubscription,
    ): void {
        const message: HubMessage = {
            type: HubMessageType.Subscribe,
            queryId,
            payload: subscription.request,
        };
        if (this._supportsSubscriptionRevisions) {
            message.revision = subscription.revision;
        }
        this.sendMessage(message);
    }

    private sendMessage(message: HubMessage): void {
        if (this._socket?.readyState === WebSocket.OPEN) {
            this._socket.send(JSON.stringify(message));
        }
    }

    private handleMessage(rawData: string): void {
        try {
            const message = JSON.parse(rawData) as HubMessage;

            // Every received message is activity — skip keep-alive ping if data is flowing.
            this._keepAlive.recordActivity();

            switch (message.type) {
                case HubMessageType.Connected:
                    this.handleConnected(message);
                    break;
                case HubMessageType.QueryResult:
                    this.handleQueryResult(message);
                    break;
                case HubMessageType.Pong:
                    this.handlePong(message);
                    break;
                case HubMessageType.Unauthorized:
                    console.warn(`Hub: query '${message.queryId}' unauthorized`);
                    this.handleUnauthorized(message);
                    break;
                case HubMessageType.Error:
                    if (this.isMessageForCurrentSubscription(message)) {
                        console.error(
                            `Hub: query '${message.queryId}' error:`,
                            message.payload,
                        );
                    }
                    break;
            }
        } catch (error) {
            console.error('Hub: error parsing message', error);
        }
    }

    private handleConnected(message: HubMessage): void {
        if (
            message.supportsSubscriptionRevisions !== true ||
            this._supportsSubscriptionRevisions
        ) {
            return;
        }

        this._supportsSubscriptionRevisions = true;

        // The legacy subscriptions sent on socket open keep this client compatible with older servers.
        // Ordered WebSocket delivery means these revision-aware replacements are processed afterwards.
        this.sendAllSubscriptions();
    }

    private handleQueryResult(message: HubMessage): void {
        if (!message.queryId) return;

        const sub = this._subscriptions.get(message.queryId);
        if (!sub || !this.isMessageForCurrentSubscription(message)) return;

        const result = message.payload as QueryResult<any>;
        sub.callback(result);
    }

    private handleUnauthorized(message: HubMessage): void {
        if (!message.queryId) return;

        const sub = this._subscriptions.get(message.queryId);
        if (!sub || !this.isMessageForCurrentSubscription(message)) return;

        this._subscriptions.delete(message.queryId);
        sub.callback(QueryResult.unauthorized());
    }

    private isMessageForCurrentSubscription(message: HubMessage): boolean {
        if (!message.queryId) return false;

        if (!this._supportsSubscriptionRevisions) {
            return (
                message.revision === undefined &&
                this._subscriptions.has(message.queryId)
            );
        }

        return (
            this.isValidRevision(message.revision) &&
            this._subscriptions.get(message.queryId)?.revision === message.revision
        );
    }

    private isValidRevision(revision: number | undefined): revision is number {
        return revision !== undefined && Number.isSafeInteger(revision) && revision > 0;
    }

    private getNextRevision(): number {
        if (this._nextRevision >= Number.MAX_SAFE_INTEGER) {
            throw new RangeError(
                'WebSocket hub subscription revision exhausted the safe integer range',
            );
        }

        return ++this._nextRevision;
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
}
