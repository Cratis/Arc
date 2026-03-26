// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Globals } from '../Globals';
import { IObservableQueryHubConnection } from './IObservableQueryHubConnection';
import { DataReceived } from './ObservableQueryConnection';
import { IReconnectPolicy } from './IReconnectPolicy';
import { ReconnectPolicy } from './ReconnectPolicy';
import { QueryResult } from './QueryResult';
import { HubMessage, HubMessageType, SubscriptionRequest } from './WebSocketHubConnection';

/* eslint-disable @typescript-eslint/no-explicit-any */

interface ActiveSubscription {
    request: SubscriptionRequest;
    callback: DataReceived<any>;
}

/**
 * A multiplexed SSE hub connection that uses EventSource for server→client streaming
 * and fetch POST requests for client→server subscribe/unsubscribe commands.
 *
 * Protocol:
 * 1. Open EventSource to the SSE hub endpoint.
 * 2. Server sends a {@link HubMessageType.Connected} message with the connection identifier.
 * 3. Client sends POST to subscribe/unsubscribe endpoints using the connection identifier.
 * 4. Server streams {@link HubMessageType.QueryResult} messages tagged with queryId.
 * 5. When EventSource closes, server cleans up all subscriptions for this connection.
 */
export class ServerSentEventHubConnection implements IObservableQueryHubConnection {
    private _eventSource?: EventSource;
    private _connectionId?: string;
    private _disconnected = false;
    private _subscriptions: Map<string, ActiveSubscription> = new Map();
    private _pendingSubscriptions: Map<string, ActiveSubscription> = new Map();
    private _lastPongLatency: number = 0;
    private _latencySamples: number[] = [];

    /**
     * Initializes a new instance of {@link ServerSentEventHubConnection}.
     * @param {string} _sseUrl The SSE hub endpoint URL (e.g. `http://localhost:5000/.cratis/queries/sse`).
     * @param {string} _subscribeUrl The subscribe POST endpoint URL.
     * @param {string} _unsubscribeUrl The unsubscribe POST endpoint URL.
     * @param {string} _microservice The microservice name to pass as a query argument.
     * @param {IReconnectPolicy} _policy The reconnect policy to use (default: {@link ReconnectPolicy}).
     */
    constructor(
        private readonly _sseUrl: string,
        private readonly _subscribeUrl: string,
        private readonly _unsubscribeUrl: string,
        private readonly _microservice: string,
        private readonly _policy: IReconnectPolicy = new ReconnectPolicy()
    ) {
    }

    /** @inheritdoc */
    get queryCount(): number {
        return this._subscriptions.size;
    }

    /** @inheritdoc */
    get lastPingLatency(): number {
        return this._lastPongLatency;
    }

    /** @inheritdoc */
    get averageLatency(): number {
        if (this._latencySamples.length === 0) return 0;
        return this._latencySamples.reduce((a, b) => a + b, 0) / this._latencySamples.length;
    }

    /** @inheritdoc */
    subscribe(queryId: string, request: SubscriptionRequest, callback: DataReceived<any>): void {
        const sub: ActiveSubscription = { request, callback };
        this._subscriptions.set(queryId, sub);

        this.ensureConnected();

        if (this._connectionId) {
            this.sendSubscribe(queryId, request);
        } else {
            // Not yet connected, queue for when Connected message arrives.
            this._pendingSubscriptions.set(queryId, sub);
        }
    }

    /** @inheritdoc */
    unsubscribe(queryId: string): void {
        this._subscriptions.delete(queryId);
        this._pendingSubscriptions.delete(queryId);

        if (this._connectionId) {
            this.sendUnsubscribe(queryId);
        }

        if (this._subscriptions.size === 0) {
            this.close();
        }
    }

    /** @inheritdoc */
    dispose(): void {
        this._disconnected = true;
        this._subscriptions.clear();
        this._pendingSubscriptions.clear();
        this._policy.cancel();
        this._eventSource?.close();
        this._eventSource = undefined;
        this._connectionId = undefined;
    }

    private ensureConnected(): void {
        if (this._disconnected) {
            this._disconnected = false;
        }

        if (this._eventSource && this._eventSource.readyState !== EventSource.CLOSED) {
            return;
        }

        this.openEventSource();
    }

    private close(): void {
        this._disconnected = true;
        this._policy.cancel();
        this._eventSource?.close();
        this._eventSource = undefined;
        this._connectionId = undefined;
    }

    private openEventSource(): void {
        let url = this._sseUrl;
        if (this._microservice?.length > 0) {
            const param = `${Globals.microserviceWSQueryArgument}=${encodeURIComponent(this._microservice)}`;
            url += (url.includes('?') ? '&' : '?') + param;
        }

        this._connectionId = undefined;
        this._eventSource = new EventSource(url);

        this._eventSource.onopen = () => {
            if (this._disconnected) return;
            console.log(`SSE hub connection established: '${url}'`);
            this._policy.reset();
        };

        this._eventSource.onmessage = (event: MessageEvent) => {
            if (this._disconnected) return;
            this.handleMessage(event.data as string);
        };

        this._eventSource.onerror = () => {
            if (this._disconnected) return;
            console.warn(`SSE hub connection error: '${url}'`);
            // Close the EventSource so the reconnect policy manages the schedule.
            this._eventSource?.close();
            this._eventSource = undefined;
            this._connectionId = undefined;
            // Move all active subscriptions to pending so they re-subscribe when
            // the next Connected message arrives after the managed reconnect.
            for (const [queryId, sub] of this._subscriptions) {
                this._pendingSubscriptions.set(queryId, sub);
            }
            if (this._subscriptions.size === 0) return;
            this._policy.schedule(() => {
                if (!this._disconnected && this._subscriptions.size > 0) {
                    this.openEventSource();
                }
            }, this._sseUrl);
        };
    }

    private handleMessage(rawData: string): void {
        try {
            const message = JSON.parse(rawData) as HubMessage;

            switch (message.type) {
                case HubMessageType.Connected:
                    this.handleConnected(message);
                    break;
                case HubMessageType.QueryResult:
                    this.handleQueryResult(message);
                    break;
                case HubMessageType.Ping:
                    // Server-sent ping; SSE is one-directional so we just record it.
                    break;
                case HubMessageType.Unauthorized:
                    console.warn(`SSE hub: query '${message.queryId}' unauthorized`);
                    break;
                case HubMessageType.Error:
                    console.error(`SSE hub: query '${message.queryId}' error:`, message.payload);
                    break;
            }
        } catch (error) {
            console.error('SSE hub: error parsing message', error);
        }
    }

    private handleConnected(message: HubMessage): void {
        this._connectionId = message.payload as string;
        console.log(`SSE hub: connected with id '${this._connectionId}'`);

        // Send all pending subscriptions now that we have a connection ID.
        for (const [queryId, sub] of this._pendingSubscriptions) {
            this.sendSubscribe(queryId, sub.request);
        }
        this._pendingSubscriptions.clear();
    }

    private handleQueryResult(message: HubMessage): void {
        if (!message.queryId) return;

        const sub = this._subscriptions.get(message.queryId);
        if (!sub) return;

        const result = message.payload as QueryResult<any>;
        sub.callback(result);
    }

    private sendSubscribe(queryId: string, request: SubscriptionRequest): void {
        if (!this._connectionId) return;

        const body = {
            connectionId: this._connectionId,
            queryId,
            request,
        };

        fetch(this._subscribeUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body),
        }).catch(error => {
            console.error(`SSE hub: subscribe POST failed for '${queryId}'`, error);
        });
    }

    private sendUnsubscribe(queryId: string): void {
        if (!this._connectionId) return;

        const body = {
            connectionId: this._connectionId,
            queryId,
        };

        fetch(this._unsubscribeUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body),
        }).catch(error => {
            console.error(`SSE hub: unsubscribe POST failed for '${queryId}'`, error);
        });
    }
}
