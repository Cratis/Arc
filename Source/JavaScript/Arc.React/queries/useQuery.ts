// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IQueryFor, QueryResultWithState, Paging, Sorting } from '@cratis/arc/queries';
import { Constructor } from '@cratis/fundamentals';
import { useState, useEffect, useContext, useRef, useMemo } from 'react';
import { SetSorting } from './SetSorting.js';
import { SetPage } from './SetPage.js';
import { SetPageSize } from './SetPageSize.js';
import { ArcContext, type ArcConfiguration } from '../ArcContext.js';
import { SliceLocation } from '@cratis/arc/reflection';
import { useRegisterSliceLocation } from '../slices/useRegisterSliceLocation.js';
import { useCommandScope } from '../commands/useCommandScope.js';
import { QueryInstanceCacheContext } from './QueryInstanceCacheContext.js';
import { serializeArgsForDependency } from './serializeArgsForDependency.js';
import { useQueryScope } from './useQueryScope.js';
import type { QueryExecution } from './QueryExecution.js';
import { buildQueryKey } from './buildQueryKey.js';
import { useResultForKey } from './useResultForKey.js';
import type { QueryDeadline } from './QueryDeadline.js';
import { runQueryExecution, type QueryPerformer } from './runQueryExecution.js';

/** Options for a single query execution. */
export interface QueryOptions {
    /** Read deadline in milliseconds. Omitted, nonpositive or nonfinite values disable the deadline. */
    timeoutMs?: number;
}

/** Options for a query hook, accepted in place of the existing isEnabled boolean. */
export interface QueryHookOptions extends QueryOptions {
    /** Whether to execute the query. Defaults to true. */
    isEnabled?: boolean;
}

/**
 * Delegate type for performing a {@link IQueryFor} in the context of the {@link useQuery} hook.
 */
export type PerformQuery<TArguments = object> = (args?: TArguments, options?: QueryOptions) => Promise<void>;

/**
 * Creates a fresh query instance configured from the Arc context, paging and sorting.
 * The instance is created first so queryName can be read: it is a hardcoded fully-qualified
 * string in generated proxies and survives minification. constructor.name is unstable
 * under minification and must not be used as a cache key.
 */
function createFreshInstance<TQuery extends IQueryFor<unknown>>(query: Constructor<TQuery>, arc: ArcConfiguration, paging: Paging, sorting: Sorting): TQuery {
    const freshInstance = new query() as TQuery;
    freshInstance.paging = paging;
    freshInstance.sorting = sorting;
    freshInstance.setMicroservice(arc.microservice);
    freshInstance.setApiBasePath(arc.apiBasePath ?? '');
    freshInstance.setOrigin(arc.origin ?? '');
    freshInstance.setHttpHeadersCallback(arc.httpHeadersCallback ?? (() => ({})));
    return freshInstance;
}

function useQueryInstance<TQuery extends IQueryFor<unknown>>(query: Constructor<TQuery>, paging: Paging, sorting: Sorting, args: object | undefined): [TQuery, string] {
    const arc = useContext(ArcContext);
    const argumentsDependency = serializeArgsForDependency(args);
    const commandScope = useCommandScope();
    const queryCache = useContext(QueryInstanceCacheContext);

    return useMemo(() => {
        const freshInstance = createFreshInstance(query, arc, paging, sorting);
        const typeName = (freshInstance as { queryName?: string }).queryName ?? query.name;
        const key = buildQueryKey(queryCache, typeName, args, { paging, sorting });

        const { instance } = queryCache.getOrCreate(key, () => freshInstance);

        // Register query with command scope
        commandScope.addQuery(instance as TQuery);

        return [instance as TQuery, key] as [TQuery, string];
    }, [query, argumentsDependency, paging, sorting, arc.microservice, arc.apiBasePath, arc.origin, commandScope]);
}

/**
 * Holds the refs tracking the outstanding execution of a query hook.
 * Unmounting retires whatever execution is outstanding, so nothing it is still waiting on can settle the
 * unmounted hook or keep its query scope performing.
 */
function useExecutionRefs() {
    const requestVersionRef = useRef(0);
    const executionRef = useRef<QueryExecution | undefined>(undefined);
    const deadlineRef = useRef<QueryDeadline | undefined>(undefined);

    useEffect(() => () => {
        executionRef.current?.retire();
        executionRef.current = undefined;
        deadlineRef.current?.dispose();
        deadlineRef.current = undefined;
    }, []);

    return { requestVersionRef, executionRef, deadlineRef };
}

/** Starts diagnostics tracking for a query key and returns the cleanup that ends it and runs the release. */
function trackQuery(arc: ArcConfiguration, key: string, owner: string | undefined, release: () => void): () => void {
    arc.observableQueryDiagnostics?.beginTracking(key, owner ?? '');
    return () => {
        arc.observableQueryDiagnostics?.endTracking(key);
        release();
    };
}

type QueryHookParameters<TQuery extends IQueryFor<TDataType>, TDataType, TArguments> = {
    query: Constructor<TQuery>;
    performer: QueryPerformer<TQuery, TDataType, TArguments>;
    sorting?: Sorting;
    paging?: Paging;
    args?: TArguments;
    isEnabled?: boolean;
    owner?: string;
    options?: QueryOptions;
};

function useQueryInternal<TDataType, TQuery extends IQueryFor<TDataType>, TArguments = object>(parameters: QueryHookParameters<TQuery, TDataType, TArguments>):
    [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting, SetPage, SetPageSize];
function useQueryInternal<TDataType, TQuery extends IQueryFor<TDataType>, TArguments = object>(parameters: QueryHookParameters<TQuery, TDataType, TArguments>):
    [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting, SetPage, SetPageSize] {
    const { query, performer, sorting, paging, args, isEnabled, owner, options } = parameters;
    const [currentPaging, setCurrentPaging] = useState<Paging>(paging ?? Paging.noPaging);
    const [currentSorting, setCurrentSorting] = useState<Sorting>(sorting ?? Sorting.none);
    const arc = useContext(ArcContext);
    useRegisterSliceLocation(isEnabled === false ? SliceLocation.empty : SliceLocation.ofType(query));
    const queryScope = useQueryScope();
    const queryCache = useContext(QueryInstanceCacheContext);
    const { requestVersionRef, executionRef, deadlineRef } = useExecutionRefs();
    const ownerRef = useRef<string | undefined>(owner);
    ownerRef.current = owner;

    // Serialized rather than compared as raw values. React compares dependencies with `Object.is`, so an
    // argument whose runtime type is an object - a `Guid`, a `DateOnly`, any generated concept - would be
    // compared by identity. A value re-derived in render position is a new object every render, which re-runs
    // the effect, which aborts the in-flight request and settles a fresh result object, which re-renders: a
    // loop that never converges. Serializing compares them by value, the same way the observable and suspense
    // hooks already do. Every argument counts, not only the required ones: the instance and the cache key follow
    // all of them, so hooks with different arguments never share an instance, a request or a cached result.
    const argumentsDependency = serializeArgsForDependency(args as object | undefined);

    const [queryInstance, cacheKey] = useQueryInstance<TQuery>(query, currentPaging, currentSorting, args as object | undefined);

    const cachedResult = queryCache.getLastResult<TDataType>(cacheKey);

    const [result, setResult] = useResultForKey<TDataType>(
        cacheKey,
        cachedResult,
        queryInstance.defaultValue,
        cached => QueryResultWithState.fromQueryResult(cached, isEnabled !== false));

    const queryExecutor = async (requestArgs?: TArguments, executionOptions?: QueryOptions) => {
        if (queryInstance) {
            await runQueryExecution<TQuery, TDataType, TArguments>({
                queryInstance,
                performer,
                args,
                options,
                argumentsDependency,
                cacheKey,
                queryCache,
                queryScope,
                requestVersionRef,
                executionRef,
                deadlineRef,
                setResult
            }, requestArgs, executionOptions);
        }
    };

    useEffect(() => {
        const key = cacheKey;

        queryCache.acquire(key);

        if (isEnabled === false) {
            return () => {
                queryCache.release(key);
            };
        }
        queryExecutor(args);

        return trackQuery(arc, key, ownerRef.current, () => queryCache.release(key));
    }, [argumentsDependency, currentPaging, currentSorting, isEnabled]);

    return [
        isEnabled === false ? QueryResultWithState.empty(queryInstance.defaultValue) : result!,
        async (args?: TArguments, executionOptions?: QueryOptions) => {
            setResult(QueryResultWithState.fromQueryResult(result!, true));
            await queryExecutor(args, executionOptions);
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
 * @param isEnabled Optional: Whether to execute the query, or options including isEnabled and timeoutMs. Defaults to enabled with no deadline.
 * @returns Tuple of {@link QueryResultWithState}, a {@link PerformQuery} delegate, and a {@link SetSorting} delegate.
 */
export function useQuery<TDataType, TQuery extends IQueryFor<TDataType>, TArguments = object>(query: Constructor<TQuery>, args?: TArguments, sorting?: Sorting, isEnabled?: boolean | QueryHookOptions):
    [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting];
export function useQuery<TDataType, TQuery extends IQueryFor<TDataType>, TArguments = object>(query: Constructor<TQuery>, args?: TArguments, sorting?: Sorting, isEnabled?: boolean | QueryHookOptions, owner?: string):
    [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting] {
    const options = typeof isEnabled === 'object' ? isEnabled : undefined;
    const enabled = typeof isEnabled === 'object' ? isEnabled.isEnabled : isEnabled;
    const [result, perform, setSorting] = useQueryInternal({ query, performer: async (queryInstance: TQuery, actualArgs?: TArguments) => await queryInstance.perform(actualArgs!), sorting, args, isEnabled: enabled, owner, options });
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
 * @param isEnabled Optional: Whether to execute the query, or options including isEnabled and timeoutMs. Defaults to enabled with no deadline.
 * @returns Tuple of {@link QueryResult} and a {@link PerformQuery} delegate.
 */
export function useQueryWithPaging<TDataType, TQuery extends IQueryFor<TDataType>, TArguments = object>(query: Constructor<TQuery>, paging: Paging, args?: TArguments, sorting?: Sorting, isEnabled?: boolean | QueryHookOptions):
    [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting, SetPage, SetPageSize];
export function useQueryWithPaging<TDataType, TQuery extends IQueryFor<TDataType>, TArguments = object>(query: Constructor<TQuery>, paging: Paging, args?: TArguments, sorting?: Sorting, isEnabled?: boolean | QueryHookOptions, owner?: string):
    [QueryResultWithState<TDataType>, PerformQuery<TArguments>, SetSorting, SetPage, SetPageSize] {
    const options = typeof isEnabled === 'object' ? isEnabled : undefined;
    const enabled = typeof isEnabled === 'object' ? isEnabled.isEnabled : isEnabled;
    return useQueryInternal({ query, performer: async (queryInstance: TQuery, actualArgs?: TArguments) => await queryInstance.perform(actualArgs!), sorting, paging, args, isEnabled: enabled, owner, options });
}
