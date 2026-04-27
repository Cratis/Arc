// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IObservableQueryConnection } from './IObservableQueryConnection';
import { IObservableQueryHubConnection } from './IObservableQueryHubConnection';
import { DataReceived } from './ObservableQueryConnection';
import { SubscriptionRequest } from './WebSocketHubConnection';
import { Globals } from '../Globals';

/* eslint-disable @typescript-eslint/no-explicit-any */

/**
 * The WebSocket demultiplexer route used when connecting through the multiplexed observable query endpoint.
 */
export const WS_HUB_ROUTE = '/.cratis/queries/ws';

/**
 * Multiplexes multiple observable query subscriptions across a bounded pool of physical
 * connections (WebSocket or SSE) to the backend demultiplexer.
 *
 * Each pool slot is a single multiplexed connection. When a new subscription is requested,
 * the slot with the fewest active queries is chosen. This balances load across N physical
 * connections while keeping the total connection count bounded.
 */
export class ObservableQueryMultiplexer {
    private readonly _connections: IObservableQueryHubConnection[];
    private readonly _size: number;

    /**
     * Initializes a new {@link ObservableQueryMultiplexer}.
     * @param {number} size Number of physical connections (pool slots).
     * @param {() => IObservableQueryHubConnection} connectionFactory Factory function to create each connection.
     */
    constructor(size: number, connectionFactory: () => IObservableQueryHubConnection) {
        this._size = Math.max(1, size);
        this._connections = Array.from({ length: this._size }, () => connectionFactory());
    }

    /**
     * Gets the pool size.
     */
    get size(): number {
        return this._size;
    }

    /**
     * Gets the best available ping latency across all connections.
     */
    get lastPingLatency(): number {
        return this._connections.reduce((min, c) =>
            c.lastPingLatency > 0 && c.lastPingLatency < min ? c.lastPingLatency : min,
            Number.MAX_SAFE_INTEGER
        );
    }

    /**
     * Gets the average latency across all connections.
     */
    get averageLatency(): number {
        const active = this._connections.filter(c => c.averageLatency > 0);
        if (active.length === 0) return 0;
        return active.reduce((sum, c) => sum + c.averageLatency, 0) / active.length;
    }

    /**
     * Subscribe to a query through the multiplexer. Picks the least-loaded connection
     * and sends a Subscribe message on it.
     * @param {string} queryName Fully qualified backend query name.
     * @param {object} queryArguments Flat query arguments (incl. page, pageSize, sortBy, sortDirection).
     * @param {DataReceived<any>} callback Callback invoked for each result.
     * @returns A cleanup function that unsubscribes from the query.
     */
    subscribe(queryName: string, queryArguments: object | undefined, callback: DataReceived<any>): () => void {
        const request = this.buildSubscriptionRequest(queryName, queryArguments);
        const conn = this.leastLoaded();
        const queryId = this.generateQueryId();

        conn.subscribe(queryId, request, callback);

        return () => {
            conn.unsubscribe(queryId);
        };
    }

    /**
     * Dispose all connections in the multiplexer.
     */
    dispose(): void {
        for (const conn of this._connections) {
            conn.dispose();
        }
    }

    private leastLoaded(): IObservableQueryHubConnection {
        return this._connections.reduce((min, c) =>
            c.queryCount < min.queryCount ? c : min, this._connections[0]);
    }

    private generateQueryId(): string {
        // Use crypto.randomUUID when available, fall back to timestamp + random
        if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
            return crypto.randomUUID();
        }
        return `${Date.now()}-${Math.random().toString(36).substring(2, 11)}`;
    }

    private buildSubscriptionRequest(queryName: string, args?: object): SubscriptionRequest {
        const request: SubscriptionRequest = { queryName };

        // Always include the transfer mode so the server knows which emission strategy to use.
        request.transferMode = Globals.observableQueryTransferMode;

        if (!args) return request;

        const a = args as Record<string, any>;
        const pagingAndSortingKeys = new Set(['page', 'pageSize', 'sortBy', 'sortDirection']);

        if (a.page !== undefined && a.page !== null) request.page = Number(a.page);
        if (a.pageSize !== undefined && a.pageSize !== null) request.pageSize = Number(a.pageSize);
        if (a.sortBy !== undefined && a.sortBy !== null) request.sortBy = String(a.sortBy);
        if (a.sortDirection !== undefined && a.sortDirection !== null) request.sortDirection = String(a.sortDirection);

        // Everything else goes into arguments as string key-value pairs
        const remaining: Record<string, string | null> = {};
        let hasRemaining = false;

        for (const [key, value] of Object.entries(a)) {
            if (pagingAndSortingKeys.has(key)) continue;
            if (value === undefined || value === null) continue;
            remaining[key] = String(value);
            hasRemaining = true;
        }

        if (hasRemaining) {
            request.arguments = remaining;
        }

        return request;
    }
}

/**
 * Wraps an {@link ObservableQueryMultiplexer} subscription as an {@link IObservableQueryConnection},
 * allowing the multiplexed transport to plug into the existing query subscription pipeline
 * without changes to callers.
 */
export class MultiplexedObservableQueryConnection<TDataType> implements IObservableQueryConnection<TDataType> {
    private _cleanup?: () => void;

    /**
     * Initializes a new {@link MultiplexedObservableQueryConnection}.
     * @param {ObservableQueryMultiplexer} multiplexer The shared multiplexer.
     * @param {string} queryName The fully qualified backend query name.
     */
    constructor(
        private readonly _pool: ObservableQueryMultiplexer,
        private readonly _queryName: string,
    ) {
    }

    /** @inheritdoc */
    get lastPingLatency(): number {
        return this._pool.lastPingLatency;
    }

    /** @inheritdoc */
    get averageLatency(): number {
        return this._pool.averageLatency;
    }

    /** @inheritdoc */
    connect(dataReceived: DataReceived<TDataType>, queryArguments?: object): void {
        this._cleanup = this._pool.subscribe(
            this._queryName,
            queryArguments,
            dataReceived as DataReceived<any>,
        );
    }

    /** @inheritdoc */
    disconnect(): void {
        this._cleanup?.();
        this._cleanup = undefined;
    }
}

// ----- Shared pool singleton management -----

let _sharedMultiplexer: ObservableQueryMultiplexer | undefined;
let _sharedMultiplexerKey = '';

/**
 * Returns the shared {@link ObservableQueryMultiplexer}, creating or re-creating it when the
 * configuration (connection count, origin, base path, microservice, transport) changes.
 * @param {() => IObservableQueryHubConnection} connectionFactory Factory to create individual connections.
 * @param {string} cacheKey A string that identifies the current configuration for invalidation.
 * @param {number} size Number of physical connections to create. Defaults to {@link Globals.queryConnectionCount}.
 * @returns The shared multiplexer instance.
 */
export function getOrCreateMultiplexer(connectionFactory: () => IObservableQueryHubConnection, cacheKey: string, size: number = Globals.queryConnectionCount): ObservableQueryMultiplexer {
    if (_sharedMultiplexer && _sharedMultiplexerKey === cacheKey) {
        return _sharedMultiplexer;
    }

    _sharedMultiplexer?.dispose();
    _sharedMultiplexer = new ObservableQueryMultiplexer(size, connectionFactory);
    _sharedMultiplexerKey = cacheKey;
    return _sharedMultiplexer;
}

/**
 * Disposes and clears the shared multiplexer singleton.
 * Intended for use in test teardown to prevent state leakage across tests.
 */
export function resetSharedMultiplexer(): void {
    _sharedMultiplexer?.dispose();
    _sharedMultiplexer = undefined;
    _sharedMultiplexerKey = '';
}
