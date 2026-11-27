// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache, resetSharedMultiplexer } from '@cratis/arc/queries';
import { invalidateSuspenseQueries } from './SuspenseQueryResources.js';
import { invalidateSuspenseObservableQueries } from './SuspenseObservableQueryResources.js';

let generation = 0;
const liveCaches = new Set<QueryInstanceCache>();
const listeners = new Set<() => void>();

/** Returns the generation shared by every Arc using the shared transport. */
export function getQueryGeneration(): number {
    return generation;
}

function resetQueryResources(): void {
    invalidateSuspenseQueries();
    invalidateSuspenseObservableQueries();
    resetSharedMultiplexer();
}

/** Registers a committed Arc. Cleanup waits for any replacement committing in the same turn. */
export function retainQueryTransport(cache: QueryInstanceCache): () => void {
    liveCaches.add(cache);
    return () => {
        liveCaches.delete(cache);
        if (liveCaches.size !== 0) return;
        const releasedGeneration = generation;
        queueMicrotask(() => {
            if (liveCaches.size === 0 && releasedGeneration === generation) resetQueryResources();
        });
    };
}

/** Subscribes Arc providers to shared reconnects. */
export function subscribeToQueryGeneration(listener: () => void): () => void {
    listeners.add(listener);
    return () => listeners.delete(listener);
}

/** Invalidates results and connections before notifying any Arc to render the new generation. */
export function reconnectQueries(): void {
    generation++;
    liveCaches.forEach(cache => cache.dispose());
    resetQueryResources();
    listeners.forEach(listener => listener());
}

/** Marks the Arc-owned cache as participating in shared reconnects. */
export class ReconnectableQueryCache extends QueryInstanceCache {}

/** Captures a guard for work associated with the current render's query version. */
export function queryVersionGuard(cache: QueryInstanceCache, version: number | undefined): () => boolean {
    return () => !(cache instanceof ReconnectableQueryCache) || getQueryGeneration() === version;
}

/** Runs cache work only while its version is current. */
export function inQueryVersion<TResult>(isCurrentVersion: () => boolean, work: () => TResult): () => TResult | undefined {
    return () => isCurrentVersion() ? work() : undefined;
}
