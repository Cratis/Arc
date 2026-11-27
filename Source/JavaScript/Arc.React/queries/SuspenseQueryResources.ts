// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { type QueryResult, QueryResultWithState } from '@cratis/arc/queries';
import { QueryFailed } from './QueryFailed.js';
import { QueryUnauthorized } from './QueryUnauthorized.js';

interface SuspenseQueryResource<T> {
    status: 'pending' | 'fulfilled' | 'rejected';
    promise: Promise<void>;
    wake: () => void;
    consumers: number;
    evictionTimer?: ReturnType<typeof setTimeout>;
    value?: QueryResultWithState<T>;
    error?: Error;
}

// Module-level cache so resources survive Suspense retries on uncommitted components.
const queryCache = new Map<string, SuspenseQueryResource<unknown>>();
const uncommittedRetentionMs = 1000;

/** Clears the Suspense query cache. Call this in test teardown to ensure test isolation. */
export function clearSuspenseQueryCache(): void {
    queryCache.forEach(resource => clearTimeout(resource.evictionTimer));
    queryCache.clear();
}

/** Evicts the previous generation and retries consumers still waiting on an old request. */
export function invalidateSuspenseQueries(): void {
    const previous = [...queryCache.values()];
    clearSuspenseQueryCache();
    previous.forEach(resource => resource.wake());
}

export function removeSuspenseQueryResource(cacheKey: string): void {
    clearTimeout(queryCache.get(cacheKey)?.evictionTimer);
    queryCache.delete(cacheKey);
}

function isUnused(cacheKey: string, resource: SuspenseQueryResource<unknown>): boolean {
    return resource.consumers === 0 && queryCache.get(cacheKey) === resource;
}

function evictIfUnused(cacheKey: string, resource: SuspenseQueryResource<unknown>): void {
    if (isUnused(cacheKey, resource)) removeSuspenseQueryResource(cacheKey);
}

export function getOrCreateSuspenseQueryResource<T>(cacheKey: string, perform: () => Promise<QueryResult<T>>): SuspenseQueryResource<T> {
    const cached = queryCache.get(cacheKey);
    if (cached) return cached as SuspenseQueryResource<T>;

    const resource = createResource(perform());
    queryCache.set(cacheKey, resource);
    const scheduleEviction = () => {
        // Let Suspense retry and commit after the request settles before evicting an unowned result.
        if (isUnused(cacheKey, resource)) {
            resource.evictionTimer = setTimeout(() => evictIfUnused(cacheKey, resource), uncommittedRetentionMs);
        }
    };
    void resource.promise.then(scheduleEviction, scheduleEviction);
    return resource;
}

export function acquireSuspenseQueryResource(cacheKey: string, resource: SuspenseQueryResource<unknown>): () => void {
    clearTimeout(resource.evictionTimer);
    resource.evictionTimer = undefined;
    resource.consumers++;
    return () => {
        if (--resource.consumers === 0) {
            // StrictMode replays the effect in the same turn, with the same resource.
            queueMicrotask(() => evictIfUnused(cacheKey, resource));
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

function createResource<T>(pendingResult: Promise<QueryResult<T>>): SuspenseQueryResource<T> {
    let wake!: () => void;
    const invalidated = new Promise<void>(resolve => { wake = resolve; });
    const resource: SuspenseQueryResource<T> = {
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
