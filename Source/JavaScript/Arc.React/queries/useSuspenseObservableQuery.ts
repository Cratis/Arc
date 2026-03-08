// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResultWithState, IObservableQueryFor, Sorting, Paging } from '@cratis/arc/queries';
import { ObservableQuerySubscription } from '@cratis/arc/queries';
import { Constructor } from '@cratis/fundamentals';
import { useState, useEffect, useContext } from 'react';
import { SetSorting } from './SetSorting';
import { SetPage } from './SetPage';
import { SetPageSize } from './SetPageSize';
import { ArcContext } from '../ArcContext';
import { QueryFailed } from './QueryFailed';
import { QueryUnauthorized } from './QueryUnauthorized';

type SuspenseStatus = 'pending' | 'fulfilled' | 'rejected';

interface ObservableSuspenseResource<T> {
    status: SuspenseStatus;
    promise: Promise<void>;
    subscription: ObservableQuerySubscription<T> | null;
    value?: QueryResultWithState<T>;
    error?: Error;
    resolve?: () => void;
    reject?: (error: Error) => void;
    listeners: Set<(value: QueryResultWithState<T>) => void>;
}

// Module-level cache so resources survive Suspense retries on uncommitted components
const _observableCache = new Map<string, ObservableSuspenseResource<unknown>>();

/**
 * Clears the Suspense observable query cache. Call this in test teardown to ensure test isolation.
 */
export function clearSuspenseObservableQueryCache(): void {
    _observableCache.forEach(resource => {
        resource.subscription?.unsubscribe();
    });
    _observableCache.clear();
}

function makeCacheKey(
    queryName: string,
    microservice: string,
    apiBasePath: string,
    origin: string,
    sorting: Sorting,
    paging: Paging,
    args: unknown
): string {
    return `${queryName}:${microservice}:${apiBasePath}:${origin}:${sorting.field ?? ''}:${sorting.direction ?? 0}:${paging.page}:${paging.pageSize}:${JSON.stringify(args)}`;
}

function useSuspenseObservableQueryInternal<TDataType, TQuery extends IObservableQueryFor<TDataType>, TArguments = object>(
    query: Constructor<TQuery>,
    sorting?: Sorting,
    paging?: Paging,
    args?: TArguments
): [QueryResultWithState<TDataType>, SetSorting, SetPage, SetPageSize] {
    const arc = useContext(ArcContext);
    const [currentSorting, setCurrentSorting] = useState<Sorting>(sorting ?? Sorting.none);
    const [currentPaging, setCurrentPaging] = useState<Paging>(paging ?? Paging.noPaging);
    const [result, setResult] = useState<QueryResultWithState<TDataType> | null>(null);

    const cacheKey = makeCacheKey(
        (query as unknown as { name: string }).name,
        arc.microservice,
        arc.apiBasePath ?? '',
        arc.origin ?? '',
        currentSorting,
        currentPaging,
        args
    );

    if (!_observableCache.has(cacheKey)) {
        const queryInstance = new query() as TQuery;
        queryInstance.sorting = currentSorting;
        queryInstance.paging = currentPaging;
        queryInstance.setMicroservice(arc.microservice);
        queryInstance.setApiBasePath(arc.apiBasePath ?? '');
        queryInstance.setOrigin(arc.origin ?? '');

        let resolvePromise!: () => void;
        let rejectPromise!: (error: Error) => void;

        const resource: ObservableSuspenseResource<TDataType> = {
            status: 'pending',
            promise: new Promise<void>((resolve, reject) => {
                resolvePromise = resolve;
                rejectPromise = reject;
            }),
            subscription: null,
            listeners: new Set()
        };

        resource.resolve = resolvePromise;
        resource.reject = rejectPromise;

        resource.subscription = queryInstance.subscribe((response) => {
            if (response.hasExceptions) {
                if (resource.status === 'pending') {
                    resource.status = 'rejected';
                    resource.error = new QueryFailed(response.exceptionMessages, response.exceptionStackTrace);
                    resource.reject?.(resource.error);
                    resource.resolve = undefined;
                    resource.reject = undefined;
                }
            } else if (!response.isAuthorized) {
                if (resource.status === 'pending') {
                    resource.status = 'rejected';
                    resource.error = new QueryUnauthorized();
                    resource.reject?.(resource.error);
                    resource.resolve = undefined;
                    resource.reject = undefined;
                }
            } else {
                const queryResult = QueryResultWithState.fromQueryResult(response, false);
                resource.value = queryResult;

                if (resource.status === 'pending') {
                    resource.status = 'fulfilled';
                    resource.resolve?.();
                    resource.resolve = undefined;
                    resource.reject = undefined;
                } else {
                    resource.listeners.forEach(listener => listener(queryResult));
                }
            }
        }, args as object);

        _observableCache.set(cacheKey, resource as ObservableSuspenseResource<unknown>);
    }

    const resource = _observableCache.get(cacheKey) as ObservableSuspenseResource<TDataType>;

    useEffect(() => {
        const handleUpdate = (value: QueryResultWithState<TDataType>) => {
            setResult(value);
        };
        resource.listeners.add(handleUpdate);

        return () => {
            resource.listeners.delete(handleUpdate);
            if (resource.listeners.size === 0) {
                resource.subscription?.unsubscribe();
                _observableCache.delete(cacheKey);
            }
        };
    }, [cacheKey, resource]);

    if (resource.status === 'rejected') {
        throw resource.error;
    }

    if (resource.status === 'pending') {
        throw resource.promise;
    }

    const resetForNewSubscription = (newCacheKey: string) => {
        const currentResource = _observableCache.get(cacheKey);
        if (currentResource) {
            currentResource.subscription?.unsubscribe();
            _observableCache.delete(cacheKey);
        }
        _observableCache.delete(newCacheKey);
        setResult(null);
    };

    return [
        result ?? resource.value!,
        async (newSorting: Sorting) => {
            const newKey = makeCacheKey(
                (query as unknown as { name: string }).name,
                arc.microservice,
                arc.apiBasePath ?? '',
                arc.origin ?? '',
                newSorting,
                currentPaging,
                args
            );
            resetForNewSubscription(newKey);
            setCurrentSorting(newSorting);
        },
        async (page: number) => {
            const newPaging = new Paging(page, currentPaging.pageSize);
            const newKey = makeCacheKey(
                (query as unknown as { name: string }).name,
                arc.microservice,
                arc.apiBasePath ?? '',
                arc.origin ?? '',
                currentSorting,
                newPaging,
                args
            );
            resetForNewSubscription(newKey);
            setCurrentPaging(newPaging);
        },
        async (pageSize: number) => {
            const newPaging = new Paging(currentPaging.page, pageSize);
            const newKey = makeCacheKey(
                (query as unknown as { name: string }).name,
                arc.microservice,
                arc.apiBasePath ?? '',
                arc.origin ?? '',
                currentSorting,
                newPaging,
                args
            );
            resetForNewSubscription(newKey);
            setCurrentPaging(newPaging);
        }
    ];
}

/**
 * React hook for working with {@link IObservableQueryFor} within React Suspense boundaries.
 * Suspends the component until the first result is received and throws errors for ErrorBoundaries.
 * @template TDataType Type of model the query is for.
 * @template TQuery Type of observable query to use.
 * @template TArguments Optional: Arguments for the query, if any
 * @param query Query type constructor.
 * @param args Optional: Arguments for the query, if any
 * @param sorting Optional: Sorting for the query.
 * @returns Tuple of {@link QueryResultWithState} and a {@link SetSorting} delegate.
 * @throws {QueryFailed} The exception that is thrown when the query has server-side exceptions.
 * @throws {QueryUnauthorized} The exception that is thrown when the query is not authorized.
 */
export function useSuspenseObservableQuery<TDataType, TQuery extends IObservableQueryFor<TDataType>, TArguments = object>(
    query: Constructor<TQuery>,
    args?: TArguments,
    sorting?: Sorting
): [QueryResultWithState<TDataType>, SetSorting] {
    const [result, setSorting] = useSuspenseObservableQueryInternal<TDataType, TQuery, TArguments>(query, sorting, Paging.noPaging, args);
    return [result, setSorting];
}

/**
 * React hook for working with {@link IObservableQueryFor} within React Suspense boundaries for queries with paging.
 * Suspends the component until the first result is received and throws errors for ErrorBoundaries.
 * @template TDataType Type of model the query is for.
 * @template TQuery Type of observable query to use.
 * @template TArguments Optional: Arguments for the query, if any
 * @param query Query type constructor.
 * @param paging Paging information.
 * @param args Optional: Arguments for the query, if any
 * @param sorting Optional: Sorting for the query.
 * @returns Tuple of {@link QueryResultWithState} and paging/sorting controls.
 * @throws {QueryFailed} The exception that is thrown when the query has server-side exceptions.
 * @throws {QueryUnauthorized} The exception that is thrown when the query is not authorized.
 */
export function useSuspenseObservableQueryWithPaging<TDataType, TQuery extends IObservableQueryFor<TDataType>, TArguments = object>(
    query: Constructor<TQuery>,
    paging: Paging,
    args?: TArguments,
    sorting?: Sorting
): [QueryResultWithState<TDataType>, SetSorting, SetPage, SetPageSize] {
    return useSuspenseObservableQueryInternal<TDataType, TQuery, TArguments>(query, sorting, paging, args);
}
