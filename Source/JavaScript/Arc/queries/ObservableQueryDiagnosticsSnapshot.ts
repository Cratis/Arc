// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Describes the state of a single physical hub connection in the multiplexer.
 */
export interface MultiplexerConnectionState {
    /** Zero-based slot index in the connection pool. */
    index: number;
    /** Whether the connection is currently established and receiving messages. */
    isConnected: boolean;
    /** Number of active query subscriptions routed through this connection. */
    queryCount: number;
}

/**
 * Multiplexer transport diagnostics.
 */
export interface MultiplexerDiagnostics {
    /** Whether at least one connection in the pool is currently connected. */
    isConnected: boolean;
    /** Number of physical connections configured (from Arc props). */
    configuredConnectionCount: number;
    /** Number of connections actually created after SSE cap logic. */
    effectiveConnectionCount: number;
    /** Number of connections that are currently connected. */
    activeConnectionCount: number;
    /** Per-connection state for the current pool. */
    connections: MultiplexerConnectionState[];
}

/**
 * A single entry in the query instance cache.
 */
export interface CacheEntryDiagnostics {
    /** The full cache key (type name + serialized args). */
    key: string;
    /** Query name parsed from the key prefix. */
    queryName: string;
    /** Number of components currently holding a reference to this entry. */
    subscriberCount: number;
    /** Number of result listeners registered on this entry. */
    listenerCount: number;
    /** Whether an active server subscription has been established. */
    subscribed: boolean;
    /** Whether a cached result has been received at least once. */
    hasResult: boolean;
    /** Estimated size of the cached result in bytes (JSON.stringify length, 0 on error). */
    estimatedBytes: number;
}

/**
 * Query instance cache diagnostics.
 */
export interface CacheDiagnostics {
    /** Whether all subscribed entries are connected (no entry has subscriberCount > 0 but subscribed == false). */
    healthy: boolean;
    /** Total number of entries in the cache (including unsubscribed retention entries). */
    entryCount: number;
    /** Total estimated size of all cached results in bytes. */
    estimatedBytes: number;
    /** Per-entry diagnostics. */
    entries: CacheEntryDiagnostics[];
}

/**
 * Transport configuration diagnostics.
 */
export interface TransportDiagnostics {
    /** The configured query transport method (e.g. ServerSentEvents or WebSocket). */
    queryTransportMethod: string;
    /** Whether direct per-query connections are used instead of the multiplexer hub. */
    queryDirectMode: boolean;
}

/**
 * Overall health summary.
 */
export interface HealthDiagnostics {
    /** True when every entry with active subscribers is also subscribed. */
    allQueriesConnected: boolean;
    /** Number of entries that have subscribers but are not currently subscribed. */
    disconnectedQueryCount: number;
}

/**
 * Ownership map entry.
 */
export interface OwnershipDiagnostics {
    /** Map of cache key → owner label (populated by opt-in owner tagging). */
    ownersByQueryKey: Record<string, string>;
    /** Map of owner label → cache keys. */
    queriesByOwner: Record<string, string[]>;
}

/**
 * A point-in-time snapshot of observable query diagnostics.
 */
export interface ObservableQueryDiagnosticsSnapshot {
    /** Overall health summary. */
    health: HealthDiagnostics;
    /** Transport configuration. */
    transport: TransportDiagnostics;
    /** Multiplexer connection pool state. */
    multiplexer: MultiplexerDiagnostics;
    /** Query instance cache state. */
    cache: CacheDiagnostics;
    /** Opt-in component ownership mapping. */
    ownership: OwnershipDiagnostics;
    /** UTC timestamp when this snapshot was taken. */
    timestamp: string;
}
