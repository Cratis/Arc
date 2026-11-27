// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache, resetSharedMultiplexer } from '@cratis/arc/queries';
import { clearSuspenseQueriesForScope } from './SuspenseQueryResources.js';
import { clearSuspenseObservableQueriesForScope } from './SuspenseObservableQueryResources.js';

let nextScope = 0;

/** Owns the cache and reconnect version for an Arc provider. */
export class ReconnectableQueryCache extends QueryInstanceCache {
    readonly suspenseScope = `arc:${nextScope++}:`;
    private _version = 0;
    private readonly _listeners = new Set<() => void>();

    readonly getVersion = () => this._version;

    readonly subscribe = (listener: () => void) => {
        this._listeners.add(listener);
        return () => this._listeners.delete(listener);
    };

    readonly reconnect = () => {
        this._version++;
        this.dispose();
        this.resetTransport();
        this._listeners.forEach(listener => listener());
    };

    readonly resetTransport = () => {
        clearSuspenseQueriesForScope(this.suspenseScope);
        clearSuspenseObservableQueriesForScope(this.suspenseScope);
        resetSharedMultiplexer();
    };
}

/** Captures a guard for work associated with the current render's query version. */
export function queryVersionGuard(cache: QueryInstanceCache, version: number | undefined): () => boolean {
    return () => !(cache instanceof ReconnectableQueryCache) || cache.getVersion() === version;
}

/** Runs cache work only while its version is current. */
export function inQueryVersion<TResult>(isCurrentVersion: () => boolean, work: () => TResult): () => TResult | undefined {
    return () => isCurrentVersion() ? work() : undefined;
}
