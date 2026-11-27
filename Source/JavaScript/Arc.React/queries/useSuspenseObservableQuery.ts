// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    QueryResultWithState,
    type IObservableQueryFor,
    type QueryInstanceCache,
    Sorting,
    Paging,
} from '@cratis/arc/queries';
import type { Constructor } from '@cratis/fundamentals';
import { useState, useContext, useId, useCallback } from 'react';
import type { SetSorting } from './SetSorting.js';
import type { SetPage } from './SetPage.js';
import type { SetPageSize } from './SetPageSize.js';
import { ArcContext } from '../ArcContext.js';
import { SliceLocation } from '@cratis/arc/reflection';
import { useRegisterSliceLocation } from '../slices/useRegisterSliceLocation.js';
import { resolveSuspenseQueryIdentity } from './resolveSuspenseQueryIdentity.js';
import { QueryInstanceCacheContext } from './QueryInstanceCacheContext.js';
import { ReconnectableQueryCache } from './ReconnectableQueryCache.js';
import { makeSuspenseQueryCacheKey } from './makeSuspenseQueryCacheKey.js';
import { createSuspenseQueryControls } from './createSuspenseQueryControls.js';
import { useSuspenseObservableResource } from './useSuspenseObservableResource.js';
import {
    ensureObservableSuspenseResource,
    getObservableSuspenseResource,
    readObservableSuspenseResource,
    registerPendingConsumer,
    unregisterPendingConsumer,
} from './SuspenseObservableQueryResources.js';

export { clearSuspenseObservableQueryCache } from './SuspenseObservableQueryResources.js';

function useVersionedResult<T>(cache: QueryInstanceCache, queryVersion: number | undefined) {
    const version = cache instanceof ReconnectableQueryCache ? queryVersion : undefined;
    const [latest, setLatest] = useState<{ version: number | undefined; result: QueryResultWithState<T> | null }>();
    const setResult = useCallback((result: QueryResultWithState<T> | null) => {
        setLatest({ version, result });
    }, [version]);
    return [latest?.version === version ? latest?.result : undefined, setResult] as const;
}

function useSuspenseObservableQueryInternal<
    TDataType,
    TQuery extends IObservableQueryFor<TDataType>,
    TArguments = object,
>(
    query: Constructor<TQuery>,
    options: { sorting?: Sorting; paging?: Paging; args?: TArguments; isEnabled: boolean },
): [QueryResultWithState<TDataType>, SetSorting, SetPage, SetPageSize] {
    const { sorting, paging, args, isEnabled } = options;
    const arc = useContext(ArcContext);
    const queryCache = useContext(QueryInstanceCacheContext);
    useRegisterSliceLocation(isEnabled === false ? SliceLocation.empty : SliceLocation.ofType(query));
    const [currentSorting, setCurrentSorting] = useState<Sorting>(sorting ?? Sorting.none);
    const [currentPaging, setCurrentPaging] = useState<Paging>(paging ?? Paging.noPaging);
    const [result, setResult] = useVersionedResult<TDataType>(queryCache, arc.queryVersion);
    const consumerId = useId();
    const queryName = resolveSuspenseQueryIdentity(query, isEnabled);
    const cacheKey = isEnabled
        ? makeSuspenseQueryCacheKey(queryName, arc, queryCache, { sorting: currentSorting, paging: currentPaging, args })
        : `__noop__${queryName}`;

    if (!isEnabled) {
        unregisterPendingConsumer(consumerId);
    }
    if (isEnabled) {
        ensureObservableSuspenseResource<TDataType>(cacheKey, {
            queryVersion: arc.queryVersion ?? 0,
            subscribe: (callback) => {
                const queryInstance = new query() as TQuery;
                queryInstance.sorting = currentSorting;
                queryInstance.paging = currentPaging;
                queryInstance.setMicroservice(arc.microservice);
                queryInstance.setApiBasePath(arc.apiBasePath ?? '');
                queryInstance.setOrigin(arc.origin ?? '');
                return queryInstance.subscribe(callback, args as object);
            },
        });
    }
    const resource = isEnabled ? getObservableSuspenseResource<TDataType>(cacheKey) : undefined;
    registerPendingConsumer(consumerId, resource);
    useSuspenseObservableResource(resource, { cacheKey, isEnabled, queryVersion: arc.queryVersion }, setResult);

    const controls = createSuspenseQueryControls({
        paging: currentPaging,
        setSorting: setCurrentSorting,
        setPaging: setCurrentPaging,
        reset: isEnabled ? () => setResult(null) : undefined,
    });
    if (!isEnabled) {
        const disabledInstance = new query();
        return [QueryResultWithState.empty(disabledInstance.defaultValue), ...controls];
    }
    const value = readObservableSuspenseResource(resource);
    return [result ?? value, ...controls];
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
    >(query, { sorting, paging: Paging.noPaging, args, isEnabled });
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
        { sorting, paging, args, isEnabled },
    );
}
