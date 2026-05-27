// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Observable, Subject } from 'rxjs';
import { QueryInstanceCache } from './QueryInstanceCache';
import { ObservableQueryMultiplexer } from './ObservableQueryMultiplexer';
import { IObservableQueryDiagnostics } from './IObservableQueryDiagnostics';
import {
    ObservableQueryDiagnosticsSnapshot,
    MultiplexerDiagnostics,
    TransportDiagnostics,
    HealthDiagnostics,
    OwnershipDiagnostics,
} from './ObservableQueryDiagnosticsSnapshot';

/**
 * Implements the {@link IObservableQueryDiagnostics} contract by collecting live state from
 * the {@link QueryInstanceCache} and the shared {@link ObservableQueryMultiplexer}.
 */
export class ObservableQueryDiagnostics implements IObservableQueryDiagnostics {
    private readonly _snapshots = new Subject<ObservableQueryDiagnosticsSnapshot>();
    private readonly _ownersByKey = new Map<string, string>();

    /**
     * Initializes a new instance of {@link ObservableQueryDiagnostics}.
     * @param _cache The {@link QueryInstanceCache} whose state will be reflected.
     * @param _getMultiplexer Factory that returns the current shared multiplexer, or {@code undefined}.
     * @param _getTransportConfig Factory that returns the current transport diagnostics.
     */
    constructor(
        private readonly _cache: QueryInstanceCache,
        private readonly _getMultiplexer: () => ObservableQueryMultiplexer | undefined,
        private readonly _getTransportConfig: () => TransportDiagnostics,
    ) {}

    /**
     * Stream of diagnostics snapshots.
     */
    get snapshots$(): Observable<ObservableQueryDiagnosticsSnapshot> {
        return this._snapshots.asObservable();
    }

    /** @inheritdoc */
    getSnapshot(): ObservableQueryDiagnosticsSnapshot {
        const snapshot = this._createSnapshot();
        this._publishSnapshot(snapshot);
        return snapshot;
    }

    /** @inheritdoc */
    beginTracking(cacheKey: string, owner: string): void {
        this._ownersByKey.set(cacheKey, owner);
        this._publishSnapshot(this._createSnapshot());
    }

    /** @inheritdoc */
    endTracking(cacheKey: string): void {
        this._ownersByKey.delete(cacheKey);
        this._publishSnapshot(this._createSnapshot());
    }

    private _buildMultiplexerDiagnostics(): MultiplexerDiagnostics {
        const mux = this._getMultiplexer();
        if (!mux) {
            return {
                isConnected: false,
                configuredConnectionCount: 0,
                effectiveConnectionCount: 0,
                activeConnectionCount: 0,
                connections: [],
            };
        }

        const connections = mux.getConnectionsSnapshot();
        const activeConnectionCount = connections.filter(c => c.isConnected).length;

        return {
            isConnected: activeConnectionCount > 0,
            configuredConnectionCount: connections.length,
            effectiveConnectionCount: connections.length,
            activeConnectionCount,
            connections,
        };
    }

    private _createSnapshot(): ObservableQueryDiagnosticsSnapshot {
        const transport = this._getTransportConfig();
        const multiplexer = this._buildMultiplexerDiagnostics();
        const cache = this._cache.getDiagnosticsSnapshot();

        const ownersByQueryKey: Record<string, string> = {};
        const queriesByOwner: Record<string, string[]> = {};

        for (const [key, owner] of this._ownersByKey) {
            ownersByQueryKey[key] = owner;
            if (!queriesByOwner[owner]) {
                queriesByOwner[owner] = [];
            }
            queriesByOwner[owner].push(key);
        }

        const ownership: OwnershipDiagnostics = { ownersByQueryKey, queriesByOwner };

        const disconnectedQueryCount = multiplexer.connections.filter(c => !c.isConnected).length;
        const health: HealthDiagnostics = {
            allQueriesConnected: multiplexer.isConnected && cache.healthy,
            disconnectedQueryCount,
        };

        return {
            health,
            transport,
            multiplexer,
            cache,
            ownership,
            timestamp: new Date().toISOString(),
        };
    }

    private _publishSnapshot(snapshot: ObservableQueryDiagnosticsSnapshot): void {
        if (typeof window !== 'undefined') {
            window.dispatchEvent(new CustomEvent('cratis:observable-query-diagnostics', { detail: snapshot }));
        }

        this._snapshots.next(snapshot);
    }
}
