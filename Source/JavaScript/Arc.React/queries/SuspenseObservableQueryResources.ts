// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { type ObservableQuerySubscription, type QueryResult, QueryResultWithState } from '@cratis/arc/queries';
import { QueryFailed } from './QueryFailed.js';
import { QueryUnauthorized } from './QueryUnauthorized.js';

export interface ObservableSuspenseResource<T> {
    readonly cacheKey: string;
    status: 'pending' | 'fulfilled' | 'rejected';
    promise: Promise<void>;
    subscription: ObservableQuerySubscription<T> | null;
    queryVersion: number;
    subscriptionGeneration: number;
    restartSubscription: () => void;
    value?: QueryResultWithState<T>;
    error?: Error;
    resolve?: () => void;
    reject?: (error: Error) => void;
    listeners: Set<(value: QueryResultWithState<T>) => void>;
    pendingConsumerIds: Set<string>;
    ownerCount: number;
    releaseScheduled: boolean;
    disposed: boolean;
    lastAccessOrder: number;
}

interface ResourceSubscription<T> {
    queryVersion: number;
    subscribe: (callback: (response: QueryResult<T>) => void) => ObservableQuerySubscription<T>;
}

const maximumUnclaimedResourceCount = 100;
const maximumPendingConsumerCountPerResource = 100;
let nextAccessOrder = 0;

// Module-level cache so resources survive Suspense retries on uncommitted components.
const observableCache = new Map<string, ObservableSuspenseResource<unknown>>();
const pendingResourceByConsumerId = new Map<string, ObservableSuspenseResource<unknown>>();

export function getObservableSuspenseResource<T>(cacheKey: string): ObservableSuspenseResource<T> | undefined {
    return observableCache.get(cacheKey) as ObservableSuspenseResource<T> | undefined;
}

export function ensureObservableSuspenseResource<T>(cacheKey: string, subscription: ResourceSubscription<T>): void {
    if (observableCache.has(cacheKey)) {
        return;
    }
    const resource = createResource<T>(cacheKey, subscription);
    observableCache.set(cacheKey, resource as ObservableSuspenseResource<unknown>);
    enforceUnclaimedResourceCapacity();
    resource.restartSubscription();
}

/** Clears the Suspense observable query cache. Call this in test teardown to ensure test isolation. */
export function clearSuspenseObservableQueryCache(): void {
    Array.from(observableCache.values()).forEach((resource) => disposeResource(resource, false));
    nextAccessOrder = 0;
}

/** Disposes the old subscriptions and retries consumers waiting for their first result. */
export function invalidateSuspenseObservableQueries(): void {
    Array.from(observableCache.values()).forEach(resource => disposeResource(resource, true));
}

function removePendingConsumer<T>(consumerId: string, resource: ObservableSuspenseResource<T>): void {
    if (pendingResourceByConsumerId.get(consumerId) === (resource as ObservableSuspenseResource<unknown>)) {
        pendingResourceByConsumerId.delete(consumerId);
    }
    resource.pendingConsumerIds.delete(consumerId);
}

function clearPendingConsumers<T>(resource: ObservableSuspenseResource<T>): void {
    resource.pendingConsumerIds.forEach((consumerId) => removePendingConsumer(consumerId, resource));
    resource.pendingConsumerIds.clear();
}

function removeCachedResource<T>(resource: ObservableSuspenseResource<T>): void {
    if (observableCache.get(resource.cacheKey) === (resource as ObservableSuspenseResource<unknown>)) {
        observableCache.delete(resource.cacheKey);
    }
}

function wakePendingResource<T>(resource: ObservableSuspenseResource<T>): void {
    if (resource.status === 'pending') {
        resource.resolve?.();
    }
}

function disposeResource<T>(resource: ObservableSuspenseResource<T>, wakeSuspense: boolean): void {
    if (resource.disposed) {
        return;
    }
    resource.disposed = true;
    resource.releaseScheduled = false;
    resource.subscription?.unsubscribe();
    resource.subscription = null;
    resource.listeners.clear();
    clearPendingConsumers(resource);
    removeCachedResource(resource);
    if (wakeSuspense) {
        wakePendingResource(resource);
    }
    resource.resolve = undefined;
    resource.reject = undefined;
}

function touchUnclaimedResource<T>(resource: ObservableSuspenseResource<T>): void {
    if (resource.disposed) {
        return;
    }
    if (resource.ownerCount > 0) {
        return;
    }
    resource.lastAccessOrder = ++nextAccessOrder;
}

function enforceUnclaimedResourceCapacity(): void {
    const unclaimedResources = Array.from(observableCache.values())
        .filter((resource) => !resource.disposed && resource.ownerCount === 0)
        .sort((left, right) => left.lastAccessOrder - right.lastAccessOrder);
    const excessResourceCount = unclaimedResources.length - maximumUnclaimedResourceCount;
    if (excessResourceCount <= 0) {
        return;
    }
    unclaimedResources.slice(0, excessResourceCount).forEach((resource) => disposeResource(resource, true));
}

function disposeIfUnclaimed<T>(resource: ObservableSuspenseResource<T>): void {
    if (resource.ownerCount !== 0) {
        return;
    }
    if (resource.pendingConsumerIds.size === 0) {
        disposeResource(resource, true);
    }
}

function releasePreviousPendingConsumer<T>(consumerId: string, resource: ObservableSuspenseResource<T>): void {
    const previousResource = pendingResourceByConsumerId.get(consumerId);
    if (previousResource === undefined) {
        return;
    }
    if (previousResource === (resource as ObservableSuspenseResource<unknown>)) {
        return;
    }
    previousResource.pendingConsumerIds.delete(consumerId);
    disposeIfUnclaimed(previousResource);
}

function trimPendingConsumers<T>(resource: ObservableSuspenseResource<T>): void {
    // Suspended renders never commit cleanup. Bound retry ids, not the ability to claim this resource.
    while (resource.pendingConsumerIds.size > maximumPendingConsumerCountPerResource) {
        const oldestConsumerId = resource.pendingConsumerIds.values().next().value as string | undefined;
        if (oldestConsumerId === undefined) {
            break;
        }
        removePendingConsumer(oldestConsumerId, resource);
    }
}

export function registerPendingConsumer<T>(consumerId: string, resource: ObservableSuspenseResource<T> | undefined): void {
    if (resource === undefined) {
        return;
    }
    if (resource.ownerCount > 0) {
        return;
    }
    releasePreviousPendingConsumer(consumerId, resource);
    pendingResourceByConsumerId.set(consumerId, resource as ObservableSuspenseResource<unknown>);
    resource.pendingConsumerIds.add(consumerId);
    trimPendingConsumers(resource);
    touchUnclaimedResource(resource);
}

export function unregisterPendingConsumer(consumerId: string): void {
    const resource = pendingResourceByConsumerId.get(consumerId);
    if (resource === undefined) {
        return;
    }
    pendingResourceByConsumerId.delete(consumerId);
    resource.pendingConsumerIds.delete(consumerId);
    disposeIfUnclaimed(resource);
}

export function claimResource<T>(resource: ObservableSuspenseResource<T>): boolean {
    if (resource.disposed) {
        return false;
    }
    if (observableCache.get(resource.cacheKey) !== (resource as ObservableSuspenseResource<unknown>)) {
        return false;
    }
    clearPendingConsumers(resource);
    resource.ownerCount++;
    resource.releaseScheduled = false;
    return true;
}

function disposeReleasedResource<T>(resource: ObservableSuspenseResource<T>): void {
    if (resource.disposed) {
        return;
    }
    if (!resource.releaseScheduled) {
        return;
    }
    if (resource.ownerCount === 0) {
        disposeResource(resource, false);
    }
}

export function releaseResource<T>(resource: ObservableSuspenseResource<T>): void {
    if (resource.disposed) {
        return;
    }
    resource.ownerCount = Math.max(0, resource.ownerCount - 1);
    if (resource.ownerCount !== 0) {
        return;
    }
    if (!resource.releaseScheduled) {
        resource.releaseScheduled = true;
        queueMicrotask(() => disposeReleasedResource(resource));
    }
}

export function reconnectResource<T>(resource: ObservableSuspenseResource<T>, queryVersion: number): void {
    if (resource.disposed) {
        return;
    }
    if (resource.queryVersion === queryVersion) {
        return;
    }
    // The shared marker permits only one restart while preserving the fulfilled value and listeners.
    resource.queryVersion = queryVersion;
    resource.restartSubscription();
}

export function readObservableSuspenseResource<T>(resource: ObservableSuspenseResource<T> | undefined): QueryResultWithState<T> {
    if (resource === undefined) {
        throw new Error('Expected an enabled suspense observable query resource');
    }
    if (resource.status === 'rejected') {
        throw resource.error;
    }
    if (resource.status === 'pending') {
        throw resource.promise;
    }
    if (resource.value === undefined) {
        throw new Error('Expected a fulfilled suspense observable query resource value');
    }
    return resource.value;
}

function receiveResponse<T>(resource: ObservableSuspenseResource<T>, response: QueryResult<T>): void {
    if (response.hasExceptions) {
        rejectPendingResource(resource, new QueryFailed(response.exceptionMessages, response.exceptionStackTrace));
    } else if (response.isAuthorized) {
        acceptResult(resource, QueryResultWithState.fromQueryResult(response, false));
    } else {
        rejectPendingResource(resource, new QueryUnauthorized());
    }
}

function acceptResult<T>(resource: ObservableSuspenseResource<T>, queryResult: QueryResultWithState<T>): void {
    resource.value = queryResult;
    if (resource.status === 'pending') {
        resource.status = 'fulfilled';
        resource.resolve?.();
        resource.resolve = undefined;
        resource.reject = undefined;
    } else {
        resource.listeners.forEach((listener) => listener(queryResult));
    }
}

function rejectPendingResource<T>(resource: ObservableSuspenseResource<T>, error: Error): void {
    if (resource.status !== 'pending') {
        return;
    }
    resource.status = 'rejected';
    resource.error = error;
    resource.reject?.(error);
    resource.resolve = undefined;
    resource.reject = undefined;
}

function receiveCurrentResponse<T>(
    resource: ObservableSuspenseResource<T>,
    generation: number,
    response: QueryResult<T>,
): void {
    if (resource.disposed) {
        return;
    }
    if (generation !== resource.subscriptionGeneration) {
        return;
    }
    if (response.isReady !== false) {
        receiveResponse(resource, response);
    }
}

function createResource<T>(cacheKey: string, subscription: ResourceSubscription<T>): ObservableSuspenseResource<T> {
    let resolvePromise!: () => void;
    let rejectPromise!: (error: Error) => void;
    const resource: ObservableSuspenseResource<T> = {
        cacheKey,
        queryVersion: subscription.queryVersion,
        subscriptionGeneration: 0,
        status: 'pending',
        promise: new Promise<void>((resolve, reject) => {
            resolvePromise = resolve;
            rejectPromise = reject;
        }),
        subscription: null,
        listeners: new Set(),
        pendingConsumerIds: new Set(),
        ownerCount: 0,
        releaseScheduled: false,
        disposed: false,
        lastAccessOrder: ++nextAccessOrder,
        restartSubscription: () => {
            const generation = ++resource.subscriptionGeneration;
            resource.subscription?.unsubscribe();
            resource.subscription = null;
            resource.subscription = subscription.subscribe((response) => receiveCurrentResponse(resource, generation, response));
        },
    };
    resource.resolve = resolvePromise;
    resource.reject = rejectPromise;
    return resource;
}
