// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IQueryFor, QueryResultWithState, Paging, Sorting } from '@cratis/arc/queries';
import { Constructor } from '@cratis/fundamentals';
import { useContext, useEffect, useReducer, useState } from 'react';
import { SetSorting } from './SetSorting';
import { SetPage } from './SetPage';
import { SetPageSize } from './SetPageSize';
import { ArcContext } from '../ArcContext';
import { useCommandScope } from '../commands/useCommandScope';
import { PerformQuery } from './useQuery';
import { QueryFailed } from './QueryFailed';
import { QueryUnauthorized } from './QueryUnauthorized';

type SuspenseStatus = 'pending' | 'fulfilled' | 'rejected';

interface SuspenseQueryResource<T> {
    status: SuspenseStatus;
    promise: Promise<void>;
    value?: QueryResultWithState<T>;
    error?: Error;
}

// Module-level cache so resources survive Suspense retries on uncommitted components
const _queryCache = new Map<string, SuspenseQueryResource<unknown>>();

/**
 * Clears the Suspense query cache. Call this in test teardown to ensure test isolation.
 */
export function clearSuspenseQueryCache(): void {
    _queryCache.clear();
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

function useSuspenseQueryInternal<TDataType, TQuery extends IQueryFor<TDataType>, TArguments = object>(
    query: Constructor<TQuery>,
    sorting?: Sorting,
    paging?: Paging,
    args?: TArguments,
    isEnabled: boolean = true
): [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting, SetPage, SetPageSize] {
    const arc = useContext(ArcContext);
    const commandScope = useCommandScope();
    const [currentSorting, setCurrentSorting] = useState<Sorting>(sorting ?? Sorting.none);
    const [currentPaging, setCurrentPaging] = useState<Paging>(paging ?? Paging.noPaging);
    const [, forceUpdate] = useReducer((x: number) => x + 1, 0);

    const cacheKey = isEnabled
        ? makeCacheKey(
            (query as unknown as { name: string }).name,
            arc.microservice,
            arc.apiBasePath ?? '',
            arc.origin ?? '',
            currentSorting,
            currentPaging,
            args)
        : `__noop__${(query as unknown as { name: string }).name}`;

    if (isEnabled && !_queryCache.has(cacheKey)) {
        const queryInstance = new query() as TQuery;
        queryInstance.sorting = currentSorting;
        queryInstance.paging = currentPaging;
        queryInstance.setMicroservice(arc.microservice);
        queryInstance.setApiBasePath(arc.apiBasePath ?? '');
        queryInstance.setOrigin(arc.origin ?? '');
        queryInstance.setHttpHeadersCallback(arc.httpHeadersCallback ?? (() => ({})));
        commandScope.addQuery(queryInstance);

        const resource: SuspenseQueryResource<TDataType> = {
            status: 'pending',
            promise: queryInstance.perform(args!).then(queryResult => {
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
        _queryCache.set(cacheKey, resource as SuspenseQueryResource<unknown>);
    }

    useEffect(() => {
        return () => {
            if (isEnabled) {
                _queryCache.delete(cacheKey);
            }
        };
    }, [cacheKey, isEnabled]);

    if (!isEnabled) {
        const disabledInstance = new query();
        return [
            QueryResultWithState.empty(disabledInstance.defaultValue),
            async () => { forceUpdate(); },
            async (newSorting: Sorting) => { setCurrentSorting(newSorting); },
            async (page: number) => { setCurrentPaging(new Paging(page, currentPaging.pageSize)); },
            async (pageSize: number) => { setCurrentPaging(new Paging(currentPaging.page, pageSize)); }
        ];
    }

    const resource = _queryCache.get(cacheKey) as SuspenseQueryResource<TDataType>;

    if (resource.status === 'rejected') {
        throw resource.error;
    }

    if (resource.status === 'pending') {
        throw resource.promise;
    }

    return [
        resource.value!,
        async () => {
            _queryCache.delete(cacheKey);
            forceUpdate();
        },
        async (newSorting: Sorting) => {
            setCurrentSorting(newSorting);
        },
        async (page: number) => {
            setCurrentPaging(new Paging(page, currentPaging.pageSize));
        },
        async (pageSize: number) => {
            setCurrentPaging(new Paging(currentPaging.page, pageSize));
        }
    ];
}

/**
 * React hook for working with {@link IQueryFor} within React Suspense boundaries.
 * Suspends the component while the query is loading and throws errors for ErrorBoundaries.
 * @template TDataType Type of model the query is for.
 * @template TQuery Type of query to use.
 * @template TArguments Optional: Arguments for the query, if any
 * @param query Query type constructor.
 * @param args Optional: Arguments for the query, if any
 * @param sorting Optional: Sorting for the query.
 * @param isEnabled Optional: Whether the query should be executed. Defaults to true. When false, the hook is a no-op and returns an empty result without suspending.
 * @returns Tuple of {@link QueryResultWithState} and a {@link PerformQuery} delegate.
 * @throws {QueryFailed} The exception that is thrown when the query has server-side exceptions.
 * @throws {QueryUnauthorized} The exception that is thrown when the query is not authorized.
 */
export function useSuspenseQuery<TDataType, TQuery extends IQueryFor<TDataType>, TArguments = object>(
    query: Constructor<TQuery>,
    args?: TArguments,
    sorting?: Sorting,
    isEnabled: boolean = true
): [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting] {
    const [result, perform, setSorting] = useSuspenseQueryInternal<TDataType, TQuery, TArguments>(query, sorting, Paging.noPaging, args, isEnabled);
    return [result, perform, setSorting];
}

/**
 * React hook for working with {@link IQueryFor} within React Suspense boundaries for queries with paging.
 * Suspends the component while the query is loading and throws errors for ErrorBoundaries.
 * @template TDataType Type of model the query is for.
 * @template TQuery Type of query to use.
 * @template TArguments Optional: Arguments for the query, if any
 * @param query Query type constructor.
 * @param paging Paging information.
 * @param args Optional: Arguments for the query, if any
 * @param sorting Optional: Sorting for the query.
 * @param isEnabled Optional: Whether the query should be executed. Defaults to true. When false, the hook is a no-op and returns an empty result without suspending.
 * @returns Tuple of {@link QueryResultWithState}, a {@link PerformQuery} delegate, and paging controls.
 * @throws {QueryFailed} The exception that is thrown when the query has server-side exceptions.
 * @throws {QueryUnauthorized} The exception that is thrown when the query is not authorized.
 */
export function useSuspenseQueryWithPaging<TDataType, TQuery extends IQueryFor<TDataType>, TArguments = object>(
    query: Constructor<TQuery>,
    paging: Paging,
    args?: TArguments,
    sorting?: Sorting,
    isEnabled: boolean = true
): [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting, SetPage, SetPageSize] {
    return useSuspenseQueryInternal<TDataType, TQuery, TArguments>(query, sorting, paging, args, isEnabled);
}
