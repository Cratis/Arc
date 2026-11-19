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
import { useState, useEffect, useContext, useRef, useMemo } from 'react';
import type { SetSorting } from './SetSorting.js';
import type { SetPage } from './SetPage.js';
import type { SetPageSize } from './SetPageSize.js';
import { ArcContext } from '../ArcContext.js';
import { SliceLocation } from '@cratis/arc/reflection';
import { useRegisterSliceLocation } from '../slices/useRegisterSliceLocation.js';
import { QueryInstanceCacheContext } from './QueryInstanceCacheContext.js';
import { serializeArgsForDependency } from './serializeArgsForDependency.js';
import { useQueryScope } from './useQueryScope.js';
import { buildQueryKey } from './buildQueryKey.js';
import { useResultForKey } from './useResultForKey.js';
import { hasAllRequiredArguments } from './hasAllRequiredArguments.js';
import { startObservableSubscription } from './startObservableSubscription.js';

function useObservableQueryInstance<TDataType, TQuery extends IObservableQueryFor<TDataType>>(
    query: Constructor<TQuery>,
    currentPaging: Paging,
    currentSorting: Sorting,
    args: object | undefined,
): [TQuery, string] {
    const arc = useContext(ArcContext);
    const queryCache = useContext(QueryInstanceCacheContext);
    const argsDependency = serializeArgsForDependency(args);

    return useMemo(() => {
        // Create the instance first to read queryName, which is a hardcoded fully-qualified
        // string in generated proxies and survives minification. constructor.name is unstable
        // under minification and must not be used as a cache key.
        const freshInstance = new query() as TQuery;
        freshInstance.paging = currentPaging;
        freshInstance.sorting = currentSorting;
        freshInstance.setMicroservice(arc.microservice);
        freshInstance.setApiBasePath(arc.apiBasePath ?? '');
        freshInstance.setOrigin(arc.origin ?? '');

        const typeName =
            (freshInstance as { queryName?: string }).queryName ?? query.name;
        const key = buildQueryKey(queryCache, typeName, args, { paging: currentPaging, sorting: currentSorting });
        const { instance } = queryCache.getOrCreate(key, () => freshInstance);

        return [instance as TQuery, key] as [TQuery, string];
    }, [
        query,
        currentPaging,
        currentSorting,
        arc.microservice,
        arc.apiBasePath,
        arc.origin,
        argsDependency,
    ]);
}

// If the cached result already exists (another subscriber already received data),
// immediately apply it to this component's state.
function applyExistingResult<TDataType>(
    queryCache: QueryInstanceCache,
    key: string,
    setResult: (result: QueryResultWithState<TDataType>) => void,
): void {
    const existing = queryCache.getLastResult<TDataType>(key);
    if (existing) {
        setResult(existing);
    }
}

type ObservableSubscriptionOptions<TQuery> = {
    queryInstance: TQuery;
    cacheKey: string;
    args: object | undefined;
    isEnabled?: boolean;
    owner?: string;
    paging: Paging;
    sorting: Sorting;
};

function useObservableSubscription<TDataType, TQuery extends IObservableQueryFor<TDataType>>(
    options: ObservableSubscriptionOptions<TQuery>,
): QueryResultWithState<TDataType> {
    const { queryInstance, cacheKey, args, isEnabled, owner, paging, sorting } = options;
    const arc = useContext(ArcContext);
    const queryCache = useContext(QueryInstanceCacheContext);
    const queryScope = useQueryScope();
    const ownerRef = useRef<string | undefined>(owner);
    ownerRef.current = owner;
    const argsDependency = serializeArgsForDependency(args);

    const cachedResult = queryCache.getLastResult<TDataType>(cacheKey);

    const [result, setResult] = useResultForKey<TDataType>(cacheKey, cachedResult, queryInstance.defaultValue, cached => cached);

    // Stable listener ref so we can add/remove the same function reference.
    const listenerRef = useRef<(r: QueryResultWithState<TDataType>) => void>();
    if (!listenerRef.current) {
        listenerRef.current = (r: QueryResultWithState<TDataType>) => setResult(r);
    }

    const hasAllRequiredArgumentsSet = hasAllRequiredArguments(
        queryInstance.requiredRequestParameters,
        args as Record<string, unknown> | undefined,
    );

    // Use all arg values (not just required ones) because the cache key includes every arg.
    // Also include arc context values so the effect re-runs and cleans up the old subscription
    // when the microservice, API base path, or origin changes.
    // Include queryVersion so that reconnectQueries() forces all hooks to re-subscribe
    // through fresh transport connections.
    const effectDeps = [
        cacheKey,
        argsDependency,
        paging,
        sorting,
        isEnabled,
        hasAllRequiredArgumentsSet,
        arc.microservice,
        arc.apiBasePath,
        arc.origin,
        arc.queryVersion,
    ];

    useEffect(() => {
        const key = cacheKey;
        const listener = listenerRef.current!;

        queryCache.acquire(key);

        if (isEnabled === false || !hasAllRequiredArgumentsSet) {
            return () => {
                queryCache.release(key);
            };
        }

        // Register this component's listener so it receives broadcasts from setLastResult.
        queryCache.addListener(key, listener);
        applyExistingResult(queryCache, key, setResult);

        // Only start a subscription if one does not already exist for this cache key.
        if (!queryCache.isSubscribed(key)) {
            startObservableSubscription<TDataType>(queryInstance, key, queryCache, queryScope, args);
        }

        arc.observableQueryDiagnostics?.beginTracking(key, ownerRef.current ?? '');
        return () => {
            arc.observableQueryDiagnostics?.endTracking(key);
            queryCache.removeListener(key, listener);
            queryCache.release(key);
        };
    }, effectDeps);

    return isEnabled === false
        ? QueryResultWithState.empty(queryInstance.defaultValue)
        : result;
}

type ObservableQueryHookOptions<TQuery, TArguments> = {
    query: Constructor<TQuery>;
    sorting?: Sorting;
    paging?: Paging;
    args?: TArguments;
    isEnabled?: boolean;
    owner?: string;
};

function useObservableQueryInternal<
    TDataType,
    TQuery extends IObservableQueryFor<TDataType>,
    TArguments = object,
>(
    options: ObservableQueryHookOptions<TQuery, TArguments>,
): [QueryResultWithState<TDataType>, SetSorting, SetPage, SetPageSize] {
    const { query, sorting, paging, args, isEnabled, owner } = options;
    const [currentPaging, setCurrentPaging] = useState<Paging>(paging ?? Paging.noPaging);
    const [currentSorting, setCurrentSorting] = useState<Sorting>(
        sorting ?? Sorting.none,
    );
    useRegisterSliceLocation(isEnabled === false ? SliceLocation.empty : SliceLocation.ofType(query));

    const [queryInstance, cacheKey] = useObservableQueryInstance<TDataType, TQuery>(
        query,
        currentPaging,
        currentSorting,
        args as object | undefined,
    );

    const result = useObservableSubscription<TDataType, TQuery>({
        queryInstance,
        cacheKey,
        args: args as object | undefined,
        isEnabled,
        owner,
        paging: currentPaging,
        sorting: currentSorting,
    });

    return [
        result,
        async (sorting: Sorting) => {
            setCurrentSorting(sorting);
        },
        async (page: number) => {
            setCurrentPaging(new Paging(page, currentPaging.pageSize));
        },
        async (pageSize: number) => {
            setCurrentPaging(new Paging(currentPaging.page, pageSize));
        },
    ];
}

/**
 * React hook for working with {@link IObservableQueryFor} within the state management of React.
 * @template TDataType Type of model the query is for.
 * @template TQuery Type of observable query to use.
 * @template TArguments Optional: Arguments for the query, if any
 * @param query Query type constructor.
 * @param args Optional: Arguments for the query, if any
 * @param sorting Optional: Sorting for the query, if any
 * @param isEnabled Optional: Whether the query should subscribe. Defaults to true. When false, the hook is a no-op and returns an empty result.
 * @returns Tuple of {@link QueryResultWithState} and a {@link PerformQuery} delegate.
 */
export function useObservableQuery<
    TDataType,
    TQuery extends IObservableQueryFor<TDataType>,
    TArguments = object,
>(
    query: Constructor<TQuery>,
    args?: TArguments,
    sorting?: Sorting,
    isEnabled?: boolean,
): [QueryResultWithState<TDataType>, SetSorting];
export function useObservableQuery<
    TDataType,
    TQuery extends IObservableQueryFor<TDataType>,
    TArguments = object,
>(
    query: Constructor<TQuery>,
    args?: TArguments,
    sorting?: Sorting,
    isEnabled?: boolean,
    owner?: string,
): [QueryResultWithState<TDataType>, SetSorting] {
    const [result, setSorting] = useObservableQueryInternal<
        TDataType,
        TQuery,
        TArguments
    >({ query, sorting, paging: Paging.noPaging, args, isEnabled, owner });
    return [result, setSorting];
}

/**
 * React hook for working with {@link IObservableQueryFor} within the state management of React for queries with paging.
 * @template TDataType Type of model the query is for.
 * @template TQuery Type of observable query to use.
 * @template TArguments Optional: Arguments for the query, if any
 * @param query Query type constructor.
 * @param paging Paging information.
 * @param args Optional: Arguments for the query, if any
 * @param sorting Optional: Sorting for the query, if any
 * @param isEnabled Optional: Whether the query should subscribe. Defaults to true. When false, the hook is a no-op and returns an empty result.
 * @returns Tuple of {@link QueryResultWithState} and paging/sorting controls.
 */
export function useObservableQueryWithPaging<
    TDataType,
    TQuery extends IObservableQueryFor<TDataType>,
    TArguments = object,
>(
    query: Constructor<TQuery>,
    paging: Paging,
    args?: TArguments,
    sorting?: Sorting,
    isEnabled?: boolean,
): [QueryResultWithState<TDataType>, SetSorting, SetPage, SetPageSize];
export function useObservableQueryWithPaging<
    TDataType,
    TQuery extends IObservableQueryFor<TDataType>,
    TArguments = object,
>(
    query: Constructor<TQuery>,
    paging: Paging,
    args?: TArguments,
    sorting?: Sorting,
    isEnabled?: boolean,
    owner?: string,
): [QueryResultWithState<TDataType>, SetSorting, SetPage, SetPageSize] {
    return useObservableQueryInternal<TDataType, TQuery, TArguments>({
        query,
        sorting,
        paging,
        args,
        isEnabled,
        owner,
    });
}
