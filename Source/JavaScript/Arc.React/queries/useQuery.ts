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
import { useQueryScope } from './useQueryScope';

/**
 * Delegate type for performing a {@link IQueryFor} in the context of the {@link useQuery} hook.
 */
export type PerformQuery<TArguments = object> = (args?: TArguments) => Promise<void>;

type QueryPerformer<TQuery extends IQueryFor<TDataType>, TDataType, TArguments = object> = (performer: TQuery, args?: TArguments) => Promise<QueryResult<TDataType>>;

/**
 * Determines whether an error is the rejection produced by aborting a request.
 * Duplicated from the query transport rather than imported, since it is internal to `@cratis/arc`.
 * @param error The error to inspect.
 * @returns True if the error represents an aborted request, false otherwise.
 */
function isAbortError(error: unknown): boolean {
    return (error as { name?: string })?.name === 'AbortError';
}

/**
 * Creates the terminal state for a query that failed before it produced a result, so the hook settles
 * as unsuccessful instead of staying on its initial - and permanently performing - state.
 * @template TDataType Type of model the query is for.
 * @param defaultValue The default value of the query, used as the data of the failed result.
 * @param error The error the query failed with.
 * @returns A {@link QueryResultWithState} describing the failure.
 */
function failedResult<TDataType>(defaultValue: TDataType, error: unknown): QueryResultWithState<TDataType> {
    const { message, stack } = error as { message?: string; stack?: string };
    return QueryResultWithState.fromQueryResult({
        ...QueryResult.noSuccess,
        data: defaultValue,
        isSuccess: false,
        isAuthorized: true,
        isValid: true,
        hasExceptions: true,
        exceptionMessages: [message ?? String(error)],
        exceptionStackTrace: stack ?? ''
    } as QueryResult<TDataType>, false);
}

function useQueryInternal<TDataType, TQuery extends IQueryFor<TDataType>, TArguments = object>(query: Constructor<TQuery>, performer: QueryPerformer<TQuery, TDataType, TArguments>, sorting?: Sorting, paging?: Paging, args?: TArguments, isEnabled?: boolean, owner?: string):
    [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting, SetPage, SetPageSize];
function useQueryInternal<TDataType, TQuery extends IQueryFor<TDataType>, TArguments = object>(query: Constructor<TQuery>, performer: QueryPerformer<TQuery, TDataType, TArguments>, sorting?: Sorting, paging?: Paging, args?: TArguments, isEnabled?: boolean, owner?: string):
    [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting, SetPage, SetPageSize] {
    const [currentPaging, setCurrentPaging] = useState<Paging>(paging ?? Paging.noPaging);
    const [currentSorting, setCurrentSorting] = useState<Sorting>(sorting ?? Sorting.none);
    const arc = useContext(ArcContext);
    const commandScope = useCommandScope();
    const queryScope = useQueryScope();
    const queryCache = useContext(QueryInstanceCacheContext);
    const cacheKeyRef = useRef<string>('');
    const ownerRef = useRef<string | undefined>(owner);
    ownerRef.current = owner;

    const queryInstance = useMemo(() => {
        // Create the instance first to read queryName, which is a hardcoded fully-qualified
        // string in generated proxies and survives minification. constructor.name is unstable
        // under minification and must not be used as a cache key.
        const freshInstance = new query() as TQuery;
        freshInstance.paging = currentPaging;
        freshInstance.sorting = currentSorting;
        freshInstance.setMicroservice(arc.microservice);
        freshInstance.setApiBasePath(arc.apiBasePath ?? '');
        freshInstance.setOrigin(arc.origin ?? '');
        freshInstance.setHttpHeadersCallback(arc.httpHeadersCallback ?? (() => ({})));

        const typeName = (freshInstance as { queryName?: string }).queryName ?? query.name;
        const key = queryCache.buildKey(typeName, args as object | undefined);
        cacheKeyRef.current = key;

        const { instance, isNew } = queryCache.getOrCreate(key, () => freshInstance);

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
            queryScope.notifyPerformingStarted();
            try {
                const queryResult = await performer(queryInstance, args);
                const withState = QueryResultWithState.fromQueryResult(queryResult, false);
                // Only a successful result is cached. The cache exists to hand the last known good
                // payload to future subscribers, so caching a failure would poison every mount for the
                // whole retention window; leaving it out means a remount shows stale-but-good data and
                // re-fetches - stale while revalidate.
                if (withState.isSuccess) {
                    queryCache.setLastResult(cacheKeyRef.current, withState);
                }
                setResult(withState);
            } catch (error) {
                // An aborted request was superseded by a newer one that now owns the result, so it is
                // discarded rather than allowed to settle over that newer result.
                if (isAbortError(error)) {
                    return;
                }
                setResult(failedResult(queryInstance.defaultValue, error));
            } finally {
                queryScope.notifyPerformingCompleted();
            }
        }
    });

    useEffect(() => {
        const key = cacheKeyRef.current;

        queryCache.acquire(key);

        if (isEnabled === false) {
            return () => {
                queryCache.release(key);
            };
        }
        queryExecutor(args);

        arc.observableQueryDiagnostics?.beginTracking(key, ownerRef.current ?? '');
        return () => {
            arc.observableQueryDiagnostics?.endTracking(key);
            queryCache.release(key);
        };
    }, [...argumentsDependency, ...[currentPaging, currentSorting, isEnabled]]);

    return [
        isEnabled === false ? QueryResultWithState.empty(queryInstance.defaultValue) : result!,
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
export function useQuery<TDataType, TQuery extends IQueryFor<TDataType>, TArguments = object>(query: Constructor<TQuery>, args?: TArguments, sorting?: Sorting, isEnabled?: boolean):
    [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting];
export function useQuery<TDataType, TQuery extends IQueryFor<TDataType>, TArguments = object>(query: Constructor<TQuery>, args?: TArguments, sorting?: Sorting, isEnabled?: boolean, owner?: string):
    [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting] {
    const [result, perform, setSorting] = useQueryInternal(query, async (queryInstance: TQuery, actualArgs?: TArguments) => await queryInstance.perform(actualArgs!), sorting, undefined, args, isEnabled, owner);
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
export function useQueryWithPaging<TDataType, TQuery extends IQueryFor<TDataType>, TArguments = object>(query: Constructor<TQuery>, paging: Paging, args?: TArguments, sorting?: Sorting, isEnabled?: boolean):
    [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting, SetPage, SetPageSize];
export function useQueryWithPaging<TDataType, TQuery extends IQueryFor<TDataType>, TArguments = object>(query: Constructor<TQuery>, paging: Paging, args?: TArguments, sorting?: Sorting, isEnabled?: boolean, owner?: string):
    [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting, SetPage, SetPageSize] {
    return useQueryInternal(query, async (queryInstance: TQuery, actualArgs?: TArguments) => await queryInstance.perform(actualArgs!), sorting, paging, args, isEnabled, owner);
}
