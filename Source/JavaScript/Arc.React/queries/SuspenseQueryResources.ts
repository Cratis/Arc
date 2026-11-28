// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { GetHttpHeaders } from '@cratis/arc';
import { type QueryResult, QueryResultWithState } from '@cratis/arc/queries';
import { fingerprintSuspenseQueryHeaders } from './fingerprintSuspenseQueryHeaders.js';
import { QueryFailed } from './QueryFailed.js';
import { QueryUnauthorized } from './QueryUnauthorized.js';

interface SuspenseQueryResource<T> {
    sourceKeys: Set<string>;
    fingerprintKey: string;
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
const maxSourceAliases = 8;
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

function getHeaderSourceKey(cacheKey: string, callback?: GetHttpHeaders): string {
    return `${getHeaderSourceId(callback)}:${cacheKey}`;
}

function getHeaderFingerprintKey(cacheKey: string, callback?: GetHttpHeaders): string {
    return `${fingerprintSuspenseQueryHeaders(callback ? new Headers(callback()) : undefined)}:${cacheKey}`;
}

/** Clears the Suspense query cache. Call this in test teardown to ensure test isolation. */
export function clearSuspenseQueryCache(): void {
    resourcesBySource.clear();
    resourcesByFingerprint.clear();
}

/** Evicts the previous generation and retries consumers still waiting on an old request. */
export function invalidateSuspenseQueries(): void {
    const previous = [...resourcesByFingerprint.values()];
    clearSuspenseQueryCache();
    previous.forEach(resource => resource.wake());
}

function registerSourceAlias(resource: SuspenseQueryResource<unknown>, sourceKey: string): void {
    resource.sourceKeys.add(sourceKey);
    resourcesBySource.set(sourceKey, resource);
    if (resource.sourceKeys.size <= maxSourceAliases) return;

    // Set iteration is insertion-ordered. Dropping an alias leaves the fingerprint entry available.
    const oldestAlias = resource.sourceKeys.values().next().value;
    if (oldestAlias !== undefined) {
        resource.sourceKeys.delete(oldestAlias);
        if (resourcesBySource.get(oldestAlias) === resource) resourcesBySource.delete(oldestAlias);
    }
}

function evictResource(resource: SuspenseQueryResource<unknown>): void {
    resource.sourceKeys.forEach(key => resourcesBySource.delete(key));
    resource.sourceKeys.clear();
    resourcesByFingerprint.delete(resource.fingerprintKey);
}

export function removeSuspenseQueryResource(cacheKey: string, callback?: GetHttpHeaders): void {
    const sourceKey = getHeaderSourceKey(cacheKey, callback);
    const fingerprintKey = getHeaderFingerprintKey(cacheKey, callback);
    const bySource = resourcesBySource.get(sourceKey);
    const byFingerprint = resourcesByFingerprint.get(fingerprintKey);
    if (bySource) evictResource(bySource);
    if (byFingerprint && byFingerprint !== bySource) evictResource(byFingerprint);
}

export function getOrCreateSuspenseQueryResource<T>(
    cacheKey: string,
    callback: GetHttpHeaders | undefined,
    perform: () => Promise<QueryResult<T>>,
): SuspenseQueryResource<T> {
    const sourceKey = getHeaderSourceKey(cacheKey, callback);
    const bySource = resourcesBySource.get(sourceKey);
    if (bySource) return bySource as SuspenseQueryResource<T>;

    // Pass a stable httpHeadersCallback (module-level or useCallback) when headers carry per-request values.
    // If retries recreate the callback and its values also change, neither index can match.
    const fingerprintKey = getHeaderFingerprintKey(cacheKey, callback);
    const byFingerprint = resourcesByFingerprint.get(fingerprintKey) as SuspenseQueryResource<T> | undefined;
    const resource = byFingerprint ?? createResource(fingerprintKey, perform());
    registerSourceAlias(resource, sourceKey);
    resourcesByFingerprint.set(fingerprintKey, resource);
    return resource;
}

export function acquireSuspenseQueryResource(resource: SuspenseQueryResource<unknown>): () => void {
    resource.consumers++;
    return () => {
        if (--resource.consumers === 0) {
            // StrictMode replays the effect in the same turn, with the same resource.
            queueMicrotask(() => {
                if (resource.consumers === 0 && resourcesByFingerprint.get(resource.fingerprintKey) === resource) {
                    evictResource(resource);
                }
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

function createResource<T>(fingerprintKey: string, pendingResult: Promise<QueryResult<T>>): SuspenseQueryResource<T> {
    let wake!: () => void;
    const invalidated = new Promise<void>(resolve => { wake = resolve; });
    const resource: SuspenseQueryResource<T> = {
        sourceKeys: new Set(),
        fingerprintKey,
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
