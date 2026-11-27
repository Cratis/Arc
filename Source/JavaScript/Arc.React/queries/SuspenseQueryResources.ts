// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { type QueryResult, QueryResultWithState } from '@cratis/arc/queries';
import { QueryFailed } from './QueryFailed.js';
import { QueryUnauthorized } from './QueryUnauthorized.js';

interface SuspenseQueryResource<T> {
    status: 'pending' | 'fulfilled' | 'rejected';
    promise: Promise<void>;
    wake: () => void;
    value?: QueryResultWithState<T>;
    error?: Error;
}

// Module-level cache so resources survive Suspense retries on uncommitted components.
const queryCache = new Map<string, SuspenseQueryResource<unknown>>();

/** Clears the Suspense query cache. Call this in test teardown to ensure test isolation. */
export function clearSuspenseQueryCache(): void {
    queryCache.clear();
}

/** Evicts the previous generation and retries consumers still waiting on an old request. */
export function invalidateSuspenseQueries(): void {
    const previous = [...queryCache.values()];
    queryCache.clear();
    previous.forEach(resource => resource.wake());
}

export function removeSuspenseQueryResource(cacheKey: string): void {
    queryCache.delete(cacheKey);
}

export function ensureSuspenseQueryResource<T>(cacheKey: string, perform: () => Promise<QueryResult<T>>): void {
    if (!queryCache.has(cacheKey)) {
        const resource = createResource(perform());
        queryCache.set(cacheKey, resource as SuspenseQueryResource<unknown>);
    }
}

export function readSuspenseQueryResource<T>(cacheKey: string): QueryResultWithState<T> {
    const resource = queryCache.get(cacheKey) as SuspenseQueryResource<T>;
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
