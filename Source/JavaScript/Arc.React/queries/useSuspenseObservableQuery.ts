// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    QueryResultWithState,
    type IObservableQueryFor,
    Sorting,
    Paging,
} from '@cratis/arc/queries';
import type { ObservableQuerySubscription } from '@cratis/arc/queries';
import type { Constructor } from '@cratis/fundamentals';
import { useState, useEffect, useContext, useId } from 'react';
import type { SetSorting } from './SetSorting';
import type { SetPage } from './SetPage';
import type { SetPageSize } from './SetPageSize';
import { ArcContext } from '../ArcContext';
import { QueryFailed } from './QueryFailed';
import { QueryUnauthorized } from './QueryUnauthorized';

type SuspenseStatus = 'pending' | 'fulfilled' | 'rejected';

interface ObservableSuspenseResource<T> {
    readonly cacheKey: string;
    status: SuspenseStatus;
    promise: Promise<void>;
    subscription: ObservableQuerySubscription<T> | null;
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

const maximumUnclaimedResourceCount = 100;
let _nextAccessOrder = 0;

// Module-level cache so resources survive Suspense retries on uncommitted components
const _observableCache = new Map<string, ObservableSuspenseResource<unknown>>();
const _pendingResourceByConsumerId = new Map<
    string,
    ObservableSuspenseResource<unknown>
>();

function clearPendingConsumers<TDataType>(
    resource: ObservableSuspenseResource<TDataType>,
): void {
    const unknownResource = resource as ObservableSuspenseResource<unknown>;
    resource.pendingConsumerIds.forEach((consumerId) => {
        if (_pendingResourceByConsumerId.get(consumerId) === unknownResource) {
            _pendingResourceByConsumerId.delete(consumerId);
        }
    });
    resource.pendingConsumerIds.clear();
}

function disposeResource<TDataType>(
    resource: ObservableSuspenseResource<TDataType>,
    wakeSuspense: boolean,
): void {
    if (resource.disposed) {
        return;
    }

    resource.disposed = true;
    resource.releaseScheduled = false;

    resource.subscription?.unsubscribe();
    resource.subscription = null;
    resource.listeners.clear();
    clearPendingConsumers(resource);

    if (
        _observableCache.get(resource.cacheKey) ===
        (resource as ObservableSuspenseResource<unknown>)
    ) {
        _observableCache.delete(resource.cacheKey);
    }

    if (wakeSuspense && resource.status === 'pending') {
        resource.resolve?.();
    }
    resource.resolve = undefined;
    resource.reject = undefined;
}

function touchUnclaimedResource<TDataType>(
    resource: ObservableSuspenseResource<TDataType>,
): void {
    if (resource.disposed || resource.ownerCount > 0) {
        return;
    }

    resource.lastAccessOrder = ++_nextAccessOrder;
}

function enforceUnclaimedResourceCapacity(): void {
    const unclaimedResources = Array.from(_observableCache.values())
        .filter((resource) => !resource.disposed && resource.ownerCount === 0)
        .sort((left, right) => left.lastAccessOrder - right.lastAccessOrder);

    const excessResourceCount =
        unclaimedResources.length - maximumUnclaimedResourceCount;
    if (excessResourceCount <= 0) {
        return;
    }

    unclaimedResources
        .slice(0, excessResourceCount)
        .forEach((resource) => disposeResource(resource, true));
}

function registerPendingConsumer<TDataType>(
    consumerId: string,
    resource: ObservableSuspenseResource<TDataType>,
): void {
    if (resource.ownerCount > 0) {
        return;
    }

    const unknownResource = resource as ObservableSuspenseResource<unknown>;
    const previousResource = _pendingResourceByConsumerId.get(consumerId);

    if (previousResource !== undefined && previousResource !== unknownResource) {
        previousResource.pendingConsumerIds.delete(consumerId);
        if (
            previousResource.ownerCount === 0 &&
            previousResource.pendingConsumerIds.size === 0
        ) {
            disposeResource(previousResource, true);
        }
    }

    _pendingResourceByConsumerId.set(consumerId, unknownResource);
    resource.pendingConsumerIds.add(consumerId);
    touchUnclaimedResource(resource);
}

function unregisterPendingConsumer(consumerId: string): void {
    const resource = _pendingResourceByConsumerId.get(consumerId);
    if (resource === undefined) {
        return;
    }

    _pendingResourceByConsumerId.delete(consumerId);
    resource.pendingConsumerIds.delete(consumerId);
    if (resource.ownerCount === 0 && resource.pendingConsumerIds.size === 0) {
        disposeResource(resource, true);
    }
}

function claimResource<TDataType>(
    resource: ObservableSuspenseResource<TDataType>,
): boolean {
    if (
        resource.disposed ||
        _observableCache.get(resource.cacheKey) !==
            (resource as ObservableSuspenseResource<unknown>)
    ) {
        return false;
    }

    clearPendingConsumers(resource);
    resource.ownerCount++;
    resource.releaseScheduled = false;
    return true;
}

function releaseResource<TDataType>(
    resource: ObservableSuspenseResource<TDataType>,
): void {
    if (resource.disposed) {
        return;
    }

    resource.ownerCount = Math.max(0, resource.ownerCount - 1);
    if (resource.ownerCount === 0 && !resource.releaseScheduled) {
        resource.releaseScheduled = true;
        queueMicrotask(() => {
            if (
                !resource.disposed &&
                resource.releaseScheduled &&
                resource.ownerCount === 0
            ) {
                disposeResource(resource, false);
            }
        });
    }
}

/**
 * Clears the Suspense observable query cache. Call this in test teardown to ensure test isolation.
 */
export function clearSuspenseObservableQueryCache(): void {
    Array.from(_observableCache.values()).forEach((resource) => {
        disposeResource(resource, false);
    });
    _nextAccessOrder = 0;
}

function makeCacheKey(
    queryName: string,
    microservice: string,
    apiBasePath: string,
    origin: string,
    sorting: Sorting,
    paging: Paging,
    args: unknown,
): string {
    return `${queryName}:${microservice}:${apiBasePath}:${origin}:${sorting.field ?? ''}:${sorting.direction ?? 0}:${paging.page}:${paging.pageSize}:${JSON.stringify(args)}`;
}

function useSuspenseObservableQueryInternal<
    TDataType,
    TQuery extends IObservableQueryFor<TDataType>,
    TArguments = object,
>(
    query: Constructor<TQuery>,
    sorting?: Sorting,
    paging?: Paging,
    args?: TArguments,
    isEnabled: boolean = true,
): [QueryResultWithState<TDataType>, SetSorting, SetPage, SetPageSize] {
    const arc = useContext(ArcContext);
    const [currentSorting, setCurrentSorting] = useState<Sorting>(
        sorting ?? Sorting.none,
    );
    const [currentPaging, setCurrentPaging] = useState<Paging>(paging ?? Paging.noPaging);
    const [result, setResult] = useState<QueryResultWithState<TDataType> | null>(null);
    const consumerId = useId();
    // SAFETY: JavaScript class constructors always expose their runtime name, which is used only as part of the cache key.
    const queryName = (query as unknown as { name: string }).name;

    const cacheKey = isEnabled
        ? makeCacheKey(
              queryName,
              arc.microservice,
              arc.apiBasePath ?? '',
              arc.origin ?? '',
              currentSorting,
              currentPaging,
              args,
          )
        : `__noop__${queryName}`;

    if (!isEnabled) {
        unregisterPendingConsumer(consumerId);
    }

    if (isEnabled && !_observableCache.has(cacheKey)) {
        const queryInstance = new query() as TQuery;
        queryInstance.sorting = currentSorting;
        queryInstance.paging = currentPaging;
        queryInstance.setMicroservice(arc.microservice);
        queryInstance.setApiBasePath(arc.apiBasePath ?? '');
        queryInstance.setOrigin(arc.origin ?? '');

        let resolvePromise!: () => void;
        let rejectPromise!: (error: Error) => void;

        const resource: ObservableSuspenseResource<TDataType> = {
            cacheKey,
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
            lastAccessOrder: ++_nextAccessOrder,
        };

        resource.resolve = resolvePromise;
        resource.reject = rejectPromise;
        _observableCache.set(cacheKey, resource as ObservableSuspenseResource<unknown>);
        enforceUnclaimedResourceCapacity();

        resource.subscription = queryInstance.subscribe((response) => {
            if (resource.disposed || response.isReady === false) {
                return;
            }

            if (response.hasExceptions) {
                if (resource.status === 'pending') {
                    resource.status = 'rejected';
                    resource.error = new QueryFailed(
                        response.exceptionMessages,
                        response.exceptionStackTrace,
                    );
                    resource.reject?.(resource.error);
                    resource.resolve = undefined;
                    resource.reject = undefined;
                }
            } else if (response.isAuthorized) {
                const queryResult = QueryResultWithState.fromQueryResult(response, false);
                resource.value = queryResult;

                if (resource.status === 'pending') {
                    resource.status = 'fulfilled';
                    resource.resolve?.();
                    resource.resolve = undefined;
                    resource.reject = undefined;
                } else {
                    resource.listeners.forEach((listener) => listener(queryResult));
                }
            } else if (resource.status === 'pending') {
                resource.status = 'rejected';
                resource.error = new QueryUnauthorized();
                resource.reject?.(resource.error);
                resource.resolve = undefined;
                resource.reject = undefined;
            }
        }, args as object);
    }

    const resource = isEnabled
        ? (_observableCache.get(cacheKey) as ObservableSuspenseResource<TDataType>)
        : undefined;

    if (resource !== undefined) {
        registerPendingConsumer(consumerId, resource);
    }

    useEffect(() => {
        if (!isEnabled || resource === undefined || !claimResource(resource)) {
            return;
        }
        const handleUpdate = (value: QueryResultWithState<TDataType>) => {
            setResult(value);
        };
        resource.listeners.add(handleUpdate);

        return () => {
            resource.listeners.delete(handleUpdate);
            releaseResource(resource);
        };
    }, [cacheKey, resource, isEnabled]);

    if (!isEnabled) {
        const disabledInstance = new query();
        return [
            QueryResultWithState.empty(disabledInstance.defaultValue),
            async (newSorting: Sorting) => {
                setCurrentSorting(newSorting);
            },
            async (page: number) => {
                setCurrentPaging(new Paging(page, currentPaging.pageSize));
            },
            async (pageSize: number) => {
                setCurrentPaging(new Paging(currentPaging.page, pageSize));
            },
        ];
    }

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

    const resetForNewSubscription = () => {
        setResult(null);
    };

    return [
        result ?? resource.value,
        async (newSorting: Sorting) => {
            resetForNewSubscription();
            setCurrentSorting(newSorting);
        },
        async (page: number) => {
            resetForNewSubscription();
            setCurrentPaging(new Paging(page, currentPaging.pageSize));
        },
        async (pageSize: number) => {
            resetForNewSubscription();
            setCurrentPaging(new Paging(currentPaging.page, pageSize));
        },
    ];
}

/**
 * React hook for working with {@link IObservableQueryFor} within React Suspense boundaries.
 * Suspends the component until the first ready result is received and throws errors for ErrorBoundaries.
 * @template TDataType Type of model the query is for.
 * @template TQuery Type of observable query to use.
 * @template TArguments Optional: Arguments for the query, if any
 * @param query Query type constructor.
 * @param args Optional: Arguments for the query, if any
 * @param sorting Optional: Sorting for the query.
 * @param isEnabled Optional: Whether the query should subscribe. Defaults to true. When false, the hook is a no-op and returns an empty result without suspending.
 * @returns Tuple of {@link QueryResultWithState} and a {@link SetSorting} delegate.
 * @throws {QueryFailed} The exception that is thrown when the query has server-side exceptions.
 * @throws {QueryUnauthorized} The exception that is thrown when the query is not authorized.
 */
export function useSuspenseObservableQuery<
    TDataType,
    TQuery extends IObservableQueryFor<TDataType>,
    TArguments = object,
>(
    query: Constructor<TQuery>,
    args?: TArguments,
    sorting?: Sorting,
    isEnabled: boolean = true,
): [QueryResultWithState<TDataType>, SetSorting] {
    const [result, setSorting] = useSuspenseObservableQueryInternal<
        TDataType,
        TQuery,
        TArguments
    >(query, sorting, Paging.noPaging, args, isEnabled);
    return [result, setSorting];
}

/**
 * React hook for working with {@link IObservableQueryFor} within React Suspense boundaries for queries with paging.
 * Suspends the component until the first ready result is received and throws errors for ErrorBoundaries.
 * @template TDataType Type of model the query is for.
 * @template TQuery Type of observable query to use.
 * @template TArguments Optional: Arguments for the query, if any
 * @param query Query type constructor.
 * @param paging Paging information.
 * @param args Optional: Arguments for the query, if any
 * @param sorting Optional: Sorting for the query.
 * @param isEnabled Optional: Whether the query should subscribe. Defaults to true. When false, the hook is a no-op and returns an empty result without suspending.
 * @returns Tuple of {@link QueryResultWithState} and paging/sorting controls.
 * @throws {QueryFailed} The exception that is thrown when the query has server-side exceptions.
 * @throws {QueryUnauthorized} The exception that is thrown when the query is not authorized.
 */
export function useSuspenseObservableQueryWithPaging<
    TDataType,
    TQuery extends IObservableQueryFor<TDataType>,
    TArguments = object,
>(
    query: Constructor<TQuery>,
    paging: Paging,
    args?: TArguments,
    sorting?: Sorting,
    isEnabled: boolean = true,
): [QueryResultWithState<TDataType>, SetSorting, SetPage, SetPageSize] {
    return useSuspenseObservableQueryInternal<TDataType, TQuery, TArguments>(
        query,
        sorting,
        paging,
        args,
        isEnabled,
    );
}
