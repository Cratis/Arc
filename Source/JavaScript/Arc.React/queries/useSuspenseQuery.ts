// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IQueryFor, QueryResultWithState, Paging, Sorting } from '@cratis/arc/queries';
import { Constructor } from '@cratis/fundamentals';
import { useContext, useEffect, useReducer, useState } from 'react';
import { SetSorting } from './SetSorting.js';
import { SetPage } from './SetPage.js';
import { SetPageSize } from './SetPageSize.js';
import { ArcContext } from '../ArcContext.js';
import { SliceLocation } from '@cratis/arc/reflection';
import { useRegisterSliceLocation } from '../slices/useRegisterSliceLocation.js';
import { useCommandScope } from '../commands/useCommandScope.js';
import { PerformQuery } from './useQuery.js';
import { resolveSuspenseQueryIdentity } from './resolveSuspenseQueryIdentity.js';
import { makeSuspenseQueryCacheKey } from './makeSuspenseQueryCacheKey.js';
import { ensureSuspenseQueryResource, readSuspenseQueryResource, removeSuspenseQueryResource } from './SuspenseQueryResources.js';

export { clearSuspenseQueryCache } from './SuspenseQueryResources.js';

function useSuspenseQueryInternal<TDataType, TQuery extends IQueryFor<TDataType>, TArguments = object>(
    query: Constructor<TQuery>,
    sorting?: Sorting,
    paging?: Paging,
    args?: TArguments,
    isEnabled: boolean = true
): [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting, SetPage, SetPageSize] {
    const arc = useContext(ArcContext);
    useRegisterSliceLocation(isEnabled === false ? SliceLocation.empty : SliceLocation.ofType(query));
    const commandScope = useCommandScope();
    const [currentSorting, setCurrentSorting] = useState<Sorting>(sorting ?? Sorting.none);
    const [currentPaging, setCurrentPaging] = useState<Paging>(paging ?? Paging.noPaging);
    const [, forceUpdate] = useReducer((x: number) => x + 1, 0);

    const cacheKey = isEnabled
        ? makeSuspenseQueryCacheKey(resolveSuspenseQueryIdentity(query), arc, {
            sorting: currentSorting,
            paging: currentPaging,
            args,
        })
        : `__noop__${(query as unknown as { name: string }).name}`;

    if (isEnabled) {
        ensureSuspenseQueryResource<TDataType>(cacheKey, () => {
            const queryInstance = new query() as TQuery;
            queryInstance.sorting = currentSorting;
            queryInstance.paging = currentPaging;
            queryInstance.setMicroservice(arc.microservice);
            queryInstance.setApiBasePath(arc.apiBasePath ?? '');
            queryInstance.setOrigin(arc.origin ?? '');
            queryInstance.setHttpHeadersCallback(arc.httpHeadersCallback ?? (() => ({})));
            commandScope.addQuery(queryInstance);
            return queryInstance.perform(args!);
        });
    }

    useEffect(() => {
        return () => {
            if (isEnabled) {
                removeSuspenseQueryResource(cacheKey);
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

    const value = readSuspenseQueryResource<TDataType>(cacheKey);

    return [
        value,
        async () => {
            removeSuspenseQueryResource(cacheKey);
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
