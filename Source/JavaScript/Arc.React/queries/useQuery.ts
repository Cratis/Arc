// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IQueryFor, QueryResultWithState, QueryResult, Paging, Sorting } from '@cratis/arc/queries';
import { Constructor } from '@cratis/fundamentals';
import { useState, useEffect, useContext, useRef, useMemo } from 'react';
import { SetSorting } from './SetSorting';
import { SetPage } from './SetPage';
import { SetPageSize } from './SetPageSize';
import { ArcContext } from '../ArcContext';
import { useCommandScope } from '../commands/useCommandScope';
import { QueryInstanceCacheContext } from './QueryInstanceCacheContext';

/**
 * Delegate type for performing a {@link IQueryFor} in the context of the {@link useQuery} hook.
 */
export type PerformQuery<TArguments = object> = (args?: TArguments) => Promise<void>;

type QueryPerformer<TQuery extends IQueryFor<TDataType>, TDataType, TArguments = object> = (performer: TQuery, args?: TArguments) => Promise<QueryResult<TDataType>>;

function useQueryInternal<TDataType, TQuery extends IQueryFor<TDataType>, TArguments = object>(query: Constructor<TQuery>, performer: QueryPerformer<TQuery, TDataType, TArguments>, sorting?: Sorting, paging?: Paging, args?: TArguments, isEnabled: boolean = true):
    [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting, SetPage, SetPageSize] {
    const [currentPaging, setCurrentPaging] = useState<Paging>(paging ?? Paging.noPaging);
    const [currentSorting, setCurrentSorting] = useState<Sorting>(sorting ?? Sorting.none);
    const arc = useContext(ArcContext);
    const commandScope = useCommandScope();
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
            instance.setHttpHeadersCallback(arc.httpHeadersCallback ?? (() => ({})));
            return instance;
        });

        if (!isNew) {
            (instance as TQuery).paging = currentPaging;
            (instance as TQuery).sorting = currentSorting;
        }

        // Register query with command scope
        commandScope.addQuery(instance as TQuery);

        return instance as TQuery;
    }, [query, currentPaging, currentSorting, arc.microservice, arc.apiBasePath, arc.origin, commandScope]);

    const cachedResult = queryCache.getLastResult<TDataType>(cacheKeyRef.current);

    const [result, setResult] = useState<QueryResultWithState<TDataType>>(
        cachedResult ?? QueryResultWithState.initial(queryInstance.defaultValue)
    );

    const argumentsDependency = queryInstance.requiredRequestParameters.map(_ => args?.[_ as keyof TArguments]);

    const queryExecutor = (async (args?: TArguments) => {
        if (queryInstance) {
            try {
                const queryResult = await performer(queryInstance, args);
                const withState = QueryResultWithState.fromQueryResult(queryResult, false);
                queryCache.setLastResult(cacheKeyRef.current, withState);
                setResult(withState);
            } catch {
                // Ignore
            }
        }
    });

    useEffect(() => {
        const key = cacheKeyRef.current;

        queryCache.acquire(key);

        if (!isEnabled) {
            return () => {
                queryCache.release(key);
            };
        }
        queryExecutor(args);

        return () => {
            queryCache.release(key);
        };
    }, [...argumentsDependency, ...[currentPaging, currentSorting, isEnabled]]);

    return [
        !isEnabled ? QueryResultWithState.empty(queryInstance.defaultValue) : result!,
        async (args?: TArguments) => {
            setResult(QueryResultWithState.fromQueryResult(result!, true));
            await queryExecutor(args);
        },
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
 * React hook for working with {@link IQueryFor} within the state management of React.
 * @template TDataType Type of model the query is for.
 * @template TQuery Type of query to use.
 * @template TArguments Optional: Arguments for the query, if any
 * @param query Query type constructor.
 * @param args Optional: Arguments for the query, if any
 * @param sorting Optional: Sorting for the query, if any
 * @param isEnabled Optional: Whether the query should be executed. Defaults to true. When false, the hook is a no-op and returns an empty result.
 * @returns Tuple of {@link QueryResultWithState}, a {@link PerformQuery} delegate, and a {@link SetSorting} delegate.
 */
export function useQuery<TDataType, TQuery extends IQueryFor<TDataType>, TArguments = object>(query: Constructor<TQuery>, args?: TArguments, sorting?: Sorting, isEnabled: boolean = true):
    [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting] {
    const [result, perform, setSorting] = useQueryInternal(query, async (queryInstance: TQuery, actualArgs?: TArguments) => await queryInstance.perform(actualArgs!), sorting, undefined, args, isEnabled);
    return [result, perform, setSorting];
}

/**
 * React hook for working with {@link IQueryFor} within the state management of React for queries with paging.
 * @template TDataType Type of model the query is for.
 * @template TQuery Type of query to use.
 * @template TArguments Optional: Arguments for the query, if any
 * @param query Query type constructor.
 * @param paging Paging information.
 * @param args Optional: Arguments for the query, if any
 * @param sorting Optional: Sorting for the query, if any
 * @param isEnabled Optional: Whether the query should be executed. Defaults to true. When false, the hook is a no-op and returns an empty result.
 * @returns Tuple of {@link QueryResult} and a {@link PerformQuery} delegate.
 */
export function useQueryWithPaging<TDataType, TQuery extends IQueryFor<TDataType>, TArguments = object>(query: Constructor<TQuery>, paging: Paging, args?: TArguments, sorting?: Sorting, isEnabled: boolean = true):
    [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting, SetPage, SetPageSize] {
    return useQueryInternal(query, async (queryInstance: TQuery, actualArgs?: TArguments) => await queryInstance.perform(actualArgs!), sorting, paging, args, isEnabled);
}
