// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResultWithState, IObservableQueryFor, Sorting, Paging } from '@cratis/arc/queries';
import { Constructor } from '@cratis/fundamentals';
import { useState, useEffect, useContext, useRef, useMemo } from 'react';
import { SetSorting } from './SetSorting';
import { SetPage } from './SetPage';
import { SetPageSize } from './SetPageSize';
import { ArcContext } from '../ArcContext';

function useObservableQueryInternal<TDataType, TQuery extends IObservableQueryFor<TDataType>, TArguments = object>(query: Constructor<TQuery>, sorting?: Sorting, paging?: Paging, args?: TArguments):
    [QueryResultWithState<TDataType>, SetSorting, SetPage, SetPageSize] {
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
        return instance;
    }, [currentPaging, currentSorting, arc.microservice, arc.apiBasePath, arc.origin]);

    const [result, setResult] = useState<QueryResultWithState<TDataType>>(QueryResultWithState.empty(queryInstance.current.defaultValue));
    const argumentsDependency = queryInstance.current.requiredRequestParameters.map(_ => args?.[_]);

    useEffect(() => {
        const subscription = queryInstance.current!.subscribe(response => {
            setResult(QueryResultWithState.fromQueryResult(response, false));
        }, args as object);

        return () => {
            subscription.unsubscribe();
        };
    }, [...argumentsDependency, ...[currentPaging, currentSorting]]);

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
        }];
}

/**
 * React hook for working with {@link IObservableQueryFor} within the state management of React.
 * @template TDataType Type of model the query is for.
 * @template TQuery Type of observable query to use.
 * @template TArguments Optional: Arguments for the query, if any
 * @param query Query type constructor.
 * @param args Optional: Arguments for the query, if any
 * @returns Tuple of {@link QueryResultWithState} and a {@link PerformQuery} delegate.
 */
export function useObservableQuery<TDataType, TQuery extends IObservableQueryFor<TDataType>, TArguments = object>(query: Constructor<TQuery>, args?: TArguments, sorting?: Sorting):
    [QueryResultWithState<TDataType>, SetSorting] {
    const [result, setSorting] = useObservableQueryInternal<TDataType, TQuery, TArguments>(query, sorting, Paging.noPaging, args);
    return [result, setSorting];
}

/**
 * React hook for working with {@link IObservableQueryFor} within the state management of React for queries with paging.
 * @template TDataType Type of model the query is for.
 * @template TQuery Type of observable query to use.
 * @template TArguments Optional: Arguments for the query, if any
 * @param query Query type constructor.
 * @param args Optional: Arguments for the query, if any
 * @param paging Paging information.
 * @returns Tuple of {@link QueryResultWithState} and a {@link PerformQuery} delegate.
 */
export function useObservableQueryWithPaging<TDataType, TQuery extends IObservableQueryFor<TDataType>, TArguments = object>(query: Constructor<TQuery>, paging: Paging, args?: TArguments, sorting?: Sorting):
    [QueryResultWithState<TDataType>, SetSorting, SetPage, SetPageSize] {
    return useObservableQueryInternal<TDataType, TQuery, TArguments>(query, sorting, paging, args);
}
