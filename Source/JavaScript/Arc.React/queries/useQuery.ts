// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IQueryFor, QueryResultWithState, QueryResult, Paging, Sorting } from '@cratis/arc/queries';
import { Constructor } from '@cratis/fundamentals';
import { useState, useEffect, useContext, useRef, useMemo } from 'react';
import { SetSorting } from './SetSorting';
import { SetPage } from './SetPage';
import { SetPageSize } from './SetPageSize';
import { ArcContext } from '../ArcContext';

/**
 * Delegate type for performing a {@link IQueryFor} in the context of the {@link useQuery} hook.
 */
export type PerformQuery<TArguments = object> = (args?: TArguments) => Promise<void>;

type QueryPerformer<TQuery extends IQueryFor<TDataType>, TDataType, TArguments = object> = (performer: TQuery, args?: TArguments) => Promise<QueryResult<TDataType>>;

function useQueryInternal<TDataType, TQuery extends IQueryFor<TDataType>, TArguments = object>(query: Constructor<TQuery>, performer: QueryPerformer<TQuery, TDataType, TArguments>, sorting?: Sorting, paging?: Paging, args?: TArguments):
    [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting, SetPage, SetPageSize] {
    const [currentPaging, setCurrentPaging] = useState<Paging>(paging ?? Paging.noPaging);
    const [currentSorting, setCurrentSorting] = useState<Sorting>(sorting ?? Sorting.none);
    const arc = useContext(ArcContext);
    const queryInstance = useRef<TQuery | null>(null);

    queryInstance.current = useMemo(() => {
        const instance = new query() as TQuery;
        instance.paging = currentPaging;
        instance.sorting = currentSorting;
        instance.setMicroservice(arc.microservice);
        instance.setApiBasePath(arc.apiBasePath ?? '');
        instance.setOrigin(arc.origin ?? '');
        instance.setHttpHeadersCallback(arc.httpHeadersCallback ?? (() => ({})));
        return instance;
    }, [currentPaging, currentSorting, arc.microservice, arc.apiBasePath, arc.origin]);

    const [result, setResult] = useState<QueryResultWithState<TDataType>>(QueryResultWithState.initial(queryInstance.current!.defaultValue));
    const argumentsDependency = queryInstance.current!.requiredRequestParameters.map(_ => args?.[_]);

    const queryExecutor = (async (args?: TArguments) => {
        if (queryInstance) {
            try {
                const queryResult = await performer(queryInstance.current!, args);
                setResult(QueryResultWithState.fromQueryResult(queryResult, false));
            } catch {
                // Ignore
            }
        }
    });

    useEffect(() => {
        queryExecutor(args);
    }, [...argumentsDependency, ...[currentPaging, currentSorting]]);

    return [
        result!,
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
 * @returns Tuple of {@link QueryResult} and a {@link PerformQuery} delegate.
 */
export function useQuery<TDataType, TQuery extends IQueryFor<TDataType>, TArguments = object>(query: Constructor<TQuery>, args?: TArguments, sorting?: Sorting):
    [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting] {
    const [result, perform, setSorting] = useQueryInternal(query, async (queryInstance: TQuery, actualArgs?: TArguments) => await queryInstance.perform(actualArgs!), sorting, undefined, args);
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
 * @returns Tuple of {@link QueryResult} and a {@link PerformQuery} delegate.
 */
export function useQueryWithPaging<TDataType, TQuery extends IQueryFor<TDataType>, TArguments = object>(query: Constructor<TQuery>, paging: Paging, args?: TArguments, sorting?: Sorting):
    [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting, SetPage, SetPageSize] {
    return useQueryInternal(query, async (queryInstance: TQuery, actualArgs?: TArguments) => await queryInstance.perform(actualArgs!), sorting, paging, args);
}
