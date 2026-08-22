// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Globals } from '../Globals';
import type { IObservableQueryHubConnection } from './IObservableQueryHubConnection';
import type { DataReceived } from './ObservableQueryConnection';
import { HubConnectionKeepAlive } from './HubConnectionKeepAlive';
import type { IReconnectPolicy } from './IReconnectPolicy';
import { ReconnectPolicy } from './ReconnectPolicy';
import { QueryResult } from './QueryResult';
import {
    type HubMessage,
    HubMessageType,
    type SubscriptionRequest,
} from './WebSocketHubConnection';

/* eslint-disable @typescript-eslint/no-explicit-any */

interface ActiveSubscription {
    request: SubscriptionRequest;
    callback: DataReceived<any>;
    revision: number;
}

/**
 * How many keep-alive intervals of silence are tolerated before the connection is considered dead.
 *
 * The server guarantees a message every interval, so this is pure slack for latency and jitter.
 * It must stay above 1 — a threshold at or below the server's own cadence makes the client and
 * server timers race, and the client tears down connections the server considers healthy.
 */
const IDLE_THRESHOLD_FACTOR = 2;

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
    private _connectTimeoutTimer?: ReturnType<typeof setTimeout>;
    private _nextRevision = 0;
    private _supportsSubscriptionRevisions = false;
    private readonly _keepAlive: HubConnectionKeepAlive;

    /**
     * Initializes a new instance of {@link ServerSentEventHubConnection}.
     * @param {string} _sseUrl The SSE hub endpoint URL (e.g. `http://localhost:5000/.cratis/queries/sse`).
     * @param {string} _subscribeUrl The subscribe POST endpoint URL.
     * @param {string} _unsubscribeUrl The unsubscribe POST endpoint URL.
     * @param {string} _microservice The microservice name to pass as a query argument.
     * @param {number} keepAliveIntervalMs The keep-alive cadence to assume until the server advertises
     *   its own on the {@link HubMessageType.Connected} message (default: 30 000 ms). The connection is
     *   considered stale after {@link IDLE_THRESHOLD_FACTOR} times this without any server message.
     * @param {number} _connectTimeoutMs How long to wait for the {@link HubMessageType.Connected}
     *   message after the HTTP connection opens before giving up and retrying (default: 15 000 ms).
     * @param {IReconnectPolicy} _policy The reconnect policy to use (default: {@link ReconnectPolicy}).
     */
    constructor(
        private readonly _sseUrl: string,
        private readonly _subscribeUrl: string,
        private readonly _unsubscribeUrl: string,
        private readonly _microservice: string,
        keepAliveIntervalMs: number = 30000,
        private readonly _connectTimeoutMs: number = 15000,
        private readonly _policy: IReconnectPolicy = new ReconnectPolicy(),
    ) {
        // SSE is server→client only: the client cannot send pings. Instead we watch for
        // inactivity — if the server stops sending messages (including its own keep-alive
        // pings) for the entire idle threshold, the connection is stale and we reconnect.
        //
        // The server guarantees a message at least every keep-alive interval, so the threshold
        // only has to absorb network latency, timer jitter and server-side scheduling hiccups.
        // A hard TCP drop surfaces immediately through `onerror`, so this watchdog only needs to
        // catch silent black-holes — that makes a generous tolerance strictly better than a tight
        // one, which would tear down healthy connections.
        //
        // The interval below is only the starting assumption; the server advertises its real
        // cadence on the Connected message and {@link handleConnected} reconfigures from it.
        this._keepAlive = new HubConnectionKeepAlive(
            keepAliveIntervalMs,
            () => {
                if (!this._disconnected && this._subscriptions.size > 0) {
                    console.warn(
                        `SSE hub: no messages received for ${this._keepAlive.idleThresholdMs}ms, reconnecting '${this._sseUrl}'`,
                    );
                    this.reconnect();
                }
            },
            keepAliveIntervalMs * IDLE_THRESHOLD_FACTOR,
        );
    }

    /** @inheritdoc */
    get queryCount(): number {
        return this._subscriptions.size;
    }

    /** @inheritdoc */
    get isConnected(): boolean {
        return (
            this._connectionId !== undefined &&
            this._eventSource?.readyState === EventSource.OPEN
        );
    }

    /** @inheritdoc */
    get lastPingLatency(): number {
        return this._lastPongLatency;
    }

    /** @inheritdoc */
    get averageLatency(): number {
        if (this._latencySamples.length === 0) return 0;
        return (
            this._latencySamples.reduce((a, b) => a + b, 0) / this._latencySamples.length
        );
    }

    /** @inheritdoc */
    subscribe(
        queryId: string,
        request: SubscriptionRequest,
        callback: DataReceived<any>,
    ): void {
        const sub: ActiveSubscription = {
            request,
            callback,
            revision: this.getNextRevision(),
        };
        this._subscriptions.set(queryId, sub);

        this.ensureConnected();

        if (this._connectionId) {
            this.sendSubscribe(queryId, sub);
        } else {
            // Not yet connected, queue for when Connected message arrives.
            this._pendingSubscriptions.set(queryId, sub);
        }
    }

    /** @inheritdoc */
    unsubscribe(queryId: string): void {
        const subscription = this._subscriptions.get(queryId);
        this._subscriptions.delete(queryId);
        this._pendingSubscriptions.delete(queryId);

        if (this._connectionId && subscription) {
            this.sendUnsubscribe(queryId, subscription.revision);
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
        this._keepAlive.stop();
        this.clearConnectTimeout();
        this.detachAndCloseEventSource();
        this._connectionId = undefined;
    }

    private detachAndCloseEventSource(): void {
        const eventSource = this._eventSource;
        if (!eventSource) return;

        eventSource.onopen = null;
        eventSource.onmessage = null;
        eventSource.onerror = null;
        eventSource.close();

        if (this._eventSource === eventSource) {
            this._eventSource = undefined;
        }
    }

    private ensureConnected(): void {
        if (this._disconnected) {
            this._disconnected = false;
        }

        if (this._eventSource && this._eventSource.readyState !== EventSource.CLOSED) {
            return;
        }

        // An immediate subscription can win while a back-off callback is pending. Cancel that callback before
        // opening, and let openEventSource perform the same live-source guard for already-queued callbacks.
        this._policy.cancel();
        this.openEventSource();
    }

    private close(): void {
        this._disconnected = true;
        this._policy.cancel();
        this._keepAlive.stop();
        this.clearConnectTimeout();
        this.detachAndCloseEventSource();
        this._connectionId = undefined;
    }

    private openEventSource(): void {
        if (this._eventSource && this._eventSource.readyState !== EventSource.CLOSED) {
            return;
        }

        this._policy.cancel();
        let url = this._sseUrl;
        if (this._microservice?.length > 0) {
            const param = `${Globals.microserviceWSQueryArgument}=${encodeURIComponent(this._microservice)}`;
            url += (url.includes('?') ? '&' : '?') + param;
        }

        this._connectionId = undefined;
        this._supportsSubscriptionRevisions = false;
        const eventSource = Globals.eventSourceFactory
            ? Globals.eventSourceFactory(url)
            : new EventSource(url);
        this._eventSource = eventSource;

        eventSource.onopen = () => {
            if (this._disconnected || this._eventSource !== eventSource) return;
            console.log(`SSE hub connection established: '${url}'`);
            this._policy.reset();
            this._keepAlive.start();

            // If the server does not send a Connected message within the timeout, the
            // connection is broken. Close and retry via the reconnect policy.
            this.clearConnectTimeout();
            this._connectTimeoutTimer = setTimeout(() => {
                if (
                    !this._disconnected &&
                    this._eventSource === eventSource &&
                    !this._connectionId
                ) {
                    console.warn(
                        `SSE hub: no Connected message within ${this._connectTimeoutMs}ms, retrying '${url}'`,
                    );
                    this.reconnect();
                }
            }, this._connectTimeoutMs);
        };

        eventSource.onmessage = (event: MessageEvent) => {
            if (this._disconnected || this._eventSource !== eventSource) return;
            this._keepAlive.recordActivity();
            this.handleMessage(event.data as string);
        };

        eventSource.onerror = () => {
            if (this._disconnected || this._eventSource !== eventSource) return;
            console.warn(`SSE hub connection error: '${url}'`);
            this.reconnect();
        };
    }

    private reconnect(): void {
        this._keepAlive.stop();
        this.clearConnectTimeout();

        // Detach before closing so callbacks already queued by the retired source are inert.
        this.detachAndCloseEventSource();
        this._connectionId = undefined;

        // Move all active subscriptions to pending so they re-subscribe when
        // the next Connected message arrives after the managed reconnect.
        for (const [queryId, sub] of this._subscriptions) {
            this._pendingSubscriptions.set(queryId, sub);
        }

        if (this._subscriptions.size === 0) return;

        this._policy.schedule(() => {
            if (!this._disconnected && this._subscriptions.size > 0) {
                this.ensureConnected();
            }
        }, this._sseUrl);
    }

    private clearConnectTimeout(): void {
        if (this._connectTimeoutTimer !== undefined) {
            clearTimeout(this._connectTimeoutTimer);
            this._connectTimeoutTimer = undefined;
        }
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
                    // Server-sent keep-alive ping — activity already recorded in onmessage.
                    break;
                case HubMessageType.Unauthorized:
                    if (this.isMessageForCurrentSubscription(message)) {
                        console.warn(`SSE hub: query '${message.queryId}' unauthorized`);
                        this.handleUnauthorized(message);
                    }
                    break;
                case HubMessageType.Error:
                    if (this.isMessageForCurrentSubscription(message)) {
                        console.error(
                            `SSE hub: query '${message.queryId}' error:`,
                            message.payload,
                        );
                    }
                    break;
            }
        } catch (error) {
            console.error('SSE hub: error parsing message', error);
        }
    }

    private handleConnected(message: HubMessage): void {
        const connectionId = message.payload as string;
        const connectionIdChanged = this._connectionId !== connectionId;
        this._connectionId = connectionId;
        this._supportsSubscriptionRevisions =
            message.supportsSubscriptionRevisions === true;
        console.log(`SSE hub: connected with id '${this._connectionId}'`);

        // Connected message arrived — cancel the connect timeout.
        this.clearConnectTimeout();

        this.applyServerKeepAliveInterval(message.keepAliveIntervalMs);

        // The active collection is authoritative. A previous connection's subscribe POST may
        // still be in flight, so relying only on pending subscriptions can orphan a query when
        // the server assigns a new connection ID before that POST completes.
        if (connectionIdChanged) {
            for (const [queryId, subscription] of this._subscriptions) {
                this.sendSubscribe(queryId, subscription);
            }
        }
        this._pendingSubscriptions.clear();
    }

    /**
     * Align the idle watchdog with the keep-alive cadence the server actually runs on.
     *
     * Without this the client assumes the default interval, so a server configured with a longer
     * interval — or with keep-alive switched off entirely — would look silent and be reconnected
     * on a loop even though it is perfectly healthy.
     * @param {number | undefined} serverIntervalMs The interval advertised by the server, if any.
     */
    private applyServerKeepAliveInterval(serverIntervalMs?: number): void {
        if (serverIntervalMs === undefined) return;

        // Keep-alive disabled server-side: silence is expected, so watching for it would guarantee
        // an endless reconnect loop. Hard drops still surface through `onerror`.
        if (serverIntervalMs <= 0) {
            this._keepAlive.stop();
            return;
        }

        this._keepAlive.reconfigure(
            serverIntervalMs,
            serverIntervalMs * IDLE_THRESHOLD_FACTOR,
        );
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
        this._pendingSubscriptions.delete(message.queryId);
        sub.callback(QueryResult.unauthorized());
    }

    private sendSubscribe(
        queryId: string,
        subscription: ActiveSubscription,
        attempt: number = 0,
    ): void {
        if (!this._connectionId || this._disconnected) return;

        // Capture the connection ID and subscription instance so retries can detect whether
        // either has been replaced while this request is in flight.
        const connectionId = this._connectionId;

        const body: {
            connectionId: string;
            queryId: string;
            request: SubscriptionRequest;
            revision?: number;
        } = {
            connectionId,
            queryId,
            request: subscription.request,
        };
        if (this._supportsSubscriptionRevisions) {
            body.revision = subscription.revision;
        }

        const customHeaders = Globals.httpHeadersCallback?.() ?? {};

        // Maximum number of subscribe retries before falling back to a full SSE reconnect.
        // In a round-robin load-balanced deployment the subscribe POST may land on a different
        // backend instance than the one holding the SSE connection.  Retrying gives the load
        // balancer the chance to route a subsequent attempt to the correct instance without
        // tearing down the SSE connection unnecessarily.  With N backend instances at most
        // N-1 retries are needed, so 3 retries covers deployments with up to 4 replicas.
        const maxRetries = 3;
        const retryDelayMs = 200;

        fetch(this._subscribeUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', ...customHeaders },
            body: JSON.stringify(body),
        })
            .then((response) => {
                if (!response.ok) {
                    if (
                        !this.isCurrentSubscription(queryId, subscription, connectionId)
                    ) {
                        console.warn(
                            `SSE hub: subscribe POST for '${queryId}' returned ${response.status} for an obsolete connection or subscription, ignoring`,
                        );
                    } else if (attempt < maxRetries) {
                        setTimeout(
                            () => {
                                if (
                                    this.isCurrentSubscription(
                                        queryId,
                                        subscription,
                                        connectionId,
                                    )
                                ) {
                                    this.sendSubscribe(
                                        queryId,
                                        subscription,
                                        attempt + 1,
                                    );
                                }
                            },
                            retryDelayMs * (attempt + 1),
                        );
                    } else {
                        console.warn(
                            `SSE hub: subscribe POST for '${queryId}' returned ${response.status} after ${attempt + 1} attempt(s), reconnecting`,
                        );
                        this.reconnect();
                    }
                }
            })
            .catch((error) => {
                if (!this.isCurrentSubscription(queryId, subscription, connectionId)) {
                    console.warn(
                        `SSE hub: subscribe POST failed for '${queryId}' on an obsolete connection or subscription, ignoring`,
                        error,
                    );
                } else if (attempt < maxRetries) {
                    setTimeout(
                        () => {
                            if (
                                this.isCurrentSubscription(
                                    queryId,
                                    subscription,
                                    connectionId,
                                )
                            ) {
                                this.sendSubscribe(queryId, subscription, attempt + 1);
                            }
                        },
                        retryDelayMs * (attempt + 1),
                    );
                } else {
                    console.error(
                        `SSE hub: subscribe POST failed for '${queryId}' after ${attempt + 1} attempt(s), reconnecting`,
                        error,
                    );
                    this.reconnect();
                }
            });
    }

    private isMessageForCurrentSubscription(message: HubMessage): boolean {
        if (!message.queryId) return false;

        const subscription = this._subscriptions.get(message.queryId);
        if (!subscription) return false;

        if (!this._supportsSubscriptionRevisions) {
            return message.revision === undefined;
        }

        return (
            this.isValidRevision(message.revision) &&
            message.revision === subscription.revision
        );
    }

    private isCurrentSubscription(
        queryId: string,
        subscription: ActiveSubscription,
        connectionId: string,
    ): boolean {
        return (
            !this._disconnected &&
            this._connectionId === connectionId &&
            this._subscriptions.get(queryId) === subscription
        );
    }

    private getNextRevision(): number {
        if (this._nextRevision >= Number.MAX_SAFE_INTEGER) {
            throw new RangeError(
                'SSE hub subscription revision exhausted the safe integer range',
            );
        }

        return ++this._nextRevision;
    }

    private isValidRevision(revision: number | undefined): revision is number {
        return revision !== undefined && Number.isSafeInteger(revision) && revision > 0;
    }

    private sendUnsubscribe(queryId: string, revision: number): void {
        if (!this._connectionId) return;

        const body: {
            connectionId: string;
            queryId: string;
            revision?: number;
        } = {
            connectionId: this._connectionId,
            queryId,
        };
        if (this._supportsSubscriptionRevisions) {
            body.revision = revision;
        }

        const customHeaders = Globals.httpHeadersCallback?.() ?? {};

        fetch(this._unsubscribeUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', ...customHeaders },
            body: JSON.stringify(body),
        }).catch((error) => {
            console.error(`SSE hub: unsubscribe POST failed for '${queryId}'`, error);
        });
    }
}
