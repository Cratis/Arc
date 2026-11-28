// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { GetHttpHeaders } from '@cratis/arc';
import { type QueryResult, QueryResultWithState } from '@cratis/arc/queries';
import { fingerprintSuspenseQueryHeaders } from './fingerprintSuspenseQueryHeaders.js';
import { QueryFailed } from './QueryFailed.js';
import { QueryUnauthorized } from './QueryUnauthorized.js';

interface ResourceKeys {
    source: string;
    fingerprint: string;
}

interface SuspenseQueryResource<T> {
    keys: ResourceKeys;
    status: 'pending' | 'fulfilled' | 'rejected';
    promise: Promise<void>;
    wake: () => void;
    consumers: number;
    value?: QueryResultWithState<T>;
    error?: Error;
}

// A settled result can still be needed by a boundary with other pending children.
const resourcesBySource = new Map<string, SuspenseQueryResource<unknown>>();
const resourcesByFingerprint = new Map<string, SuspenseQueryResource<unknown>>();
const headerSourceIds = new WeakMap<GetHttpHeaders, number>();
let nextHeaderSourceId = 1;

function getHeaderSourceId(callback?: GetHttpHeaders): number {
    if (!callback) return 0;
    let id = headerSourceIds.get(callback);
    if (id === undefined) {
        id = nextHeaderSourceId++;
        headerSourceIds.set(callback, id);
    }
    return id;
}

function lookupResource(cacheKey: string, callback?: GetHttpHeaders) {
    const source = `${getHeaderSourceId(callback)}:${cacheKey}`;
    const bySource = resourcesBySource.get(source);
    if (bySource) return { keys: bySource.keys, cached: bySource };

    // Pass a stable httpHeadersCallback (module-level or useCallback) when headers carry per-request values.
    // If retries recreate the callback and its values also change, neither index can match.
    const fingerprint = `${fingerprintSuspenseQueryHeaders(callback ? new Headers(callback()) : undefined)}:${cacheKey}`;
    return { keys: { source, fingerprint }, cached: resourcesByFingerprint.get(fingerprint) };
}

/** Clears the Suspense query cache. Call this in test teardown to ensure test isolation. */
export function clearSuspenseQueryCache(): void {
    resourcesBySource.clear();
    resourcesByFingerprint.clear();
}

/** Evicts the previous generation and retries consumers still waiting on an old request. */
export function invalidateSuspenseQueries(): void {
    const previous = [...resourcesBySource.values()];
    clearSuspenseQueryCache();
    previous.forEach(resource => resource.wake());
}

export function removeSuspenseQueryResource(resource: SuspenseQueryResource<unknown>): void {
    const { source, fingerprint } = resource.keys;
    if (resourcesBySource.get(source) === resource) resourcesBySource.delete(source);
    if (resourcesByFingerprint.get(fingerprint) === resource) resourcesByFingerprint.delete(fingerprint);
}

export function getOrCreateSuspenseQueryResource<T>(
    cacheKey: string,
    callback: GetHttpHeaders | undefined,
    perform: () => Promise<QueryResult<T>>,
): SuspenseQueryResource<T> {
    const { keys, cached } = lookupResource(cacheKey, callback);
    if (cached) return cached as SuspenseQueryResource<T>;

    const resource = createResource(keys, perform());
    resourcesBySource.set(keys.source, resource);
    resourcesByFingerprint.set(keys.fingerprint, resource);
    return resource;
}

export function acquireSuspenseQueryResource(resource: SuspenseQueryResource<unknown>): () => void {
    resource.consumers++;
    return () => {
        if (--resource.consumers === 0) {
            // StrictMode replays the effect in the same turn, with the same resource.
            queueMicrotask(() => {
                if (resource.consumers === 0) removeSuspenseQueryResource(resource);
            });
        }
    };
}

export function readSuspenseQueryResource<T>(resource: SuspenseQueryResource<T>): QueryResultWithState<T> {
    if (resource.status === 'rejected') {
        throw resource.error;
    }
    if (resource.status === 'pending') {
        throw resource.promise;
    }
    return resource.value!;
}

function createResource<T>(keys: ResourceKeys, pendingResult: Promise<QueryResult<T>>): SuspenseQueryResource<T> {
    let wake!: () => void;
    const invalidated = new Promise<void>(resolve => { wake = resolve; });
    const resource: SuspenseQueryResource<T> = {
        keys,
        status: 'pending',
        wake,
        consumers: 0,
        promise: Promise.race([pendingResult, invalidated]).then(queryResult => {
            if (!queryResult) return;
            if (queryResult.hasExceptions) {
                resource.status = 'rejected';
                resource.error = new QueryFailed(queryResult.exceptionMessages, queryResult.exceptionStackTrace);
            } else if (!queryResult.isAuthorized) {
                resource.status = 'rejected';
                resource.error = new QueryUnauthorized();
            } else {
                resource.status = 'fulfilled';
                resource.value = QueryResultWithState.fromQueryResult(queryResult, false);
            }
        })
    };
    return resource;
}
