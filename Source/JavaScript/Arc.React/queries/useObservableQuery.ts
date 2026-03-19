// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResultWithState, IObservableQueryFor, Sorting, Paging } from '@cratis/arc/queries';
import { Constructor } from '@cratis/fundamentals';
import { useState, useEffect, useContext, useRef, useMemo } from 'react';
import { SetSorting } from './SetSorting';
import { SetPage } from './SetPage';
import { SetPageSize } from './SetPageSize';
import { ArcContext } from '../ArcContext';
import { QueryInstanceCacheContext } from './QueryInstanceCacheContext';

function useObservableQueryInternal<TDataType, TQuery extends IObservableQueryFor<TDataType>, TArguments = object>(query: Constructor<TQuery>, sorting?: Sorting, paging?: Paging, args?: TArguments, isEnabled: boolean = true):
    [QueryResultWithState<TDataType>, SetSorting, SetPage, SetPageSize] {
    const [currentPaging, setCurrentPaging] = useState<Paging>(paging ?? Paging.noPaging);
    const [currentSorting, setCurrentSorting] = useState<Sorting>(sorting ?? Sorting.none);
    const arc = useContext(ArcContext);
    const queryCache = useContext(QueryInstanceCacheContext);
    const cacheKeyRef = useRef<string>('');

    const queryInstance = useMemo(() => {
        const key = queryCache.buildKey(query.name, args as object | undefined);
        cacheKeyRef.current = key;

        const { instance, isNew } = queryCache.getOrCreate(key, () => {
            const instance = new query() as TQuery;
            instance.paging = currentPaging;
            instance.sorting = currentSorting;
            instance.setMicroservice(arc.microservice);
            instance.setApiBasePath(arc.apiBasePath ?? '');
            instance.setOrigin(arc.origin ?? '');
            return instance;
        });

        if (!isNew) {
            // Update mutable settings on the shared instance
            (instance as TQuery).paging = currentPaging;
            (instance as TQuery).sorting = currentSorting;
        }

        return instance as TQuery;
    }, [currentPaging, currentSorting, arc.microservice, arc.apiBasePath, arc.origin, ...(args ? Object.values(args) : [])]);

    const cachedResult = queryCache.getLastResult<TDataType>(cacheKeyRef.current);

    const [result, setResult] = useState<QueryResultWithState<TDataType>>(
        cachedResult ?? QueryResultWithState.initial(queryInstance.defaultValue)
    );

    const argumentsDependency = queryInstance.requiredRequestParameters.map(_ => args?.[_ as keyof TArguments]);

    useEffect(() => {
        const key = cacheKeyRef.current;

        if (!isEnabled) {
            return () => {
                queryCache.release(key);
            };
        }

        const subscription = queryInstance.subscribe(response => {
            const withState = QueryResultWithState.fromQueryResult(response, false);
            queryCache.setLastResult(key, withState);
            setResult(withState);
        }, args as object);

        return () => {
            subscription.unsubscribe();
            queryCache.release(key);
        };
    }, [...argumentsDependency, ...[currentPaging, currentSorting, isEnabled]]);

    return [
        !isEnabled ? QueryResultWithState.empty(queryInstance.defaultValue) : result,
        async (sorting: Sorting) => {
            setCurrentSorting(sorting);
        },
        async (page: number) => {
            setCurrentPaging(new Paging(page, currentPaging.pageSize));
        },
        async (pageSize: number) => {
            setCurrentPaging(new Paging(currentPaging.page, pageSize));
        }];
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
export function useObservableQuery<TDataType, TQuery extends IObservableQueryFor<TDataType>, TArguments = object>(query: Constructor<TQuery>, args?: TArguments, sorting?: Sorting, isEnabled: boolean = true):
    [QueryResultWithState<TDataType>, SetSorting] {
    const [result, setSorting] = useObservableQueryInternal<TDataType, TQuery, TArguments>(query, sorting, Paging.noPaging, args, isEnabled);
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
export function useObservableQueryWithPaging<TDataType, TQuery extends IObservableQueryFor<TDataType>, TArguments = object>(query: Constructor<TQuery>, paging: Paging, args?: TArguments, sorting?: Sorting, isEnabled: boolean = true):
    [QueryResultWithState<TDataType>, SetSorting, SetPage, SetPageSize] {
    return useObservableQueryInternal<TDataType, TQuery, TArguments>(query, sorting, paging, args, isEnabled);
}
