// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IQueryFor, QueryResultWithState, QueryResult, Paging, Sorting } from '@cratis/arc/queries';
import { Constructor } from '@cratis/fundamentals';
import { useState, useEffect, useContext, useRef, useMemo } from 'react';
import { SetSorting } from './SetSorting.js';
import { SetPage } from './SetPage.js';
import { SetPageSize } from './SetPageSize.js';
import { ArcContext, type ArcConfiguration } from '../ArcContext.js';
import { useCommandScope } from '../commands/useCommandScope.js';
import { QueryInstanceCacheContext } from './QueryInstanceCacheContext.js';
import { serializeArgsForDependency } from './serializeArgsForDependency.js';
import { useQueryScope } from './useQueryScope.js';
import type { InFlightRequest } from './InFlightRequest.js';
import { type QueryExecution, startQueryExecution } from './QueryExecution.js';
import { buildQueryKey } from './buildQueryKey.js';
import { useResultForKey } from './useResultForKey.js';

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
    // Destructuring a nullish rejection value throws, which would make the `String(error)` fallback
    // written for exactly that case unreachable - so the rejection is coerced to an object first.
    const { message } = (error ?? {}) as { message?: string };
    return QueryResultWithState.fromQueryResult({
        ...QueryResult.noSuccess,
        data: defaultValue,
        isSuccess: false,
        isAuthorized: true,
        isValid: true,
        hasExceptions: true,
        exceptionMessages: [message ?? String(error)],
        // Left empty on purpose - see the matching comment in QueryFor. A stack trace here would be
        // the one that escapes the redaction every server-returned result goes through.
        exceptionStackTrace: ''
    } as QueryResult<TDataType>, false);
}

/**
 * The most recent request started on each query instance. Instances are shared between hooks through the
 * query instance cache, and starting a request aborts whatever that instance had in flight - so the latest
 * request is the one every hook on the instance is waiting for, whichever hook started it.
 */
const latestRequests = new WeakMap<object, InFlightRequest>();

/**
 * Waits for the request that superseded an aborted one and returns its outcome. A superseding request can
 * itself be superseded, so this follows the chain until a request settles with something other than an abort.
 * Only a request for the same arguments is adopted - one performed with explicitly different arguments answers
 * a different question.
 * @template TDataType Type of model the query is for.
 * @param queryInstance The shared query instance.
 * @param abortedRequest The request that was aborted.
 * @returns The superseding result, the error it failed with, or `undefined` when there is nothing to adopt.
 */
async function settleFromSupersedingRequest<TDataType>(queryInstance: object, abortedRequest: InFlightRequest): Promise<{ result?: QueryResult<TDataType>; error?: unknown } | undefined> {
    let current = abortedRequest;
    for (;;) {
        const latest = latestRequests.get(queryInstance);
        if (latest === undefined || latest === current || latest.argumentsKey !== abortedRequest.argumentsKey) {
            return undefined;
        }
        current = latest;
        try {
            return { result: await latest.request as QueryResult<TDataType> };
        } catch (error) {
            if (!isAbortError(error)) {
                return { error };
            }
        }
    }
}

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
    const requestVersionRef = useRef(0);
    const executionRef = useRef<QueryExecution | undefined>(undefined);
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

    const [queryInstance, cacheKey] = useMemo(() => {
        const freshInstance = createFreshInstance(query, arc, currentPaging, currentSorting);
        const typeName = (freshInstance as { queryName?: string }).queryName ?? query.name;
        const key = buildQueryKey(queryCache, typeName, args as object | undefined, { paging: currentPaging, sorting: currentSorting });

        const { instance } = queryCache.getOrCreate(key, () => freshInstance);

        // Register query with command scope
        commandScope.addQuery(instance as TQuery);

        return [instance as TQuery, key] as const;
    }, [query, argumentsDependency, currentPaging, currentSorting, arc.microservice, arc.apiBasePath, arc.origin, commandScope]);

    const cachedResult = queryCache.getLastResult<TDataType>(cacheKey);

    const [result, setResult] = useResultForKey<TDataType>(
        cacheKey,
        cachedResult,
        queryInstance.defaultValue,
        cached => QueryResultWithState.fromQueryResult(cached, isEnabled !== false));

    const queryExecutor = (async (requestArgs?: TArguments) => {
        if (queryInstance) {
            // A newer execution retires the one before it. The earlier one may be waiting on a request this hook
            // does not own - one a co-subscriber started - which can outlive it by any amount; left alone it would
            // keep this hook's query scope performing until that request settles. The request itself is not
            // aborted: it is shared, and the co-subscriber that started it still needs its outcome.
            executionRef.current?.retire();
            const execution = startQueryExecution(queryScope);
            executionRef.current = execution;

            // Only this hook's latest request may settle its state. An earlier one can still finish after a later
            // one has settled - for instance when the later one returned early on client validation without
            // aborting anything, or ran on another instance after the arguments changed - and it must not
            // overwrite that newer state. A retired execution - its hook unmounted - may not settle it at all.
            const version = ++requestVersionRef.current;
            const settle = (next: QueryResultWithState<TDataType> | ((current: QueryResultWithState<TDataType>) => QueryResultWithState<TDataType>)) => {
                if (!execution.isRetired && version === requestVersionRef.current) {
                    setResult(next);
                }
            };
            const argumentsKey = serializeArgsForDependency((requestArgs ?? args) as object | undefined);
            const inFlight: InFlightRequest = { request: performer(queryInstance, requestArgs), argumentsKey };
            latestRequests.set(queryInstance, inFlight);
            try {
                const queryResult = await inFlight.request as QueryResult<TDataType>;
                const withState = QueryResultWithState.fromQueryResult(queryResult, false);
                // Only a successful result is cached, and only under the key it answers - a request
                // performed with explicitly different arguments does not belong to this key. The cache
                // exists to hand the last known good payload to future subscribers, so caching a failure
                // would poison every mount for the whole retention window; leaving it out means a remount
                // shows stale-but-good data and re-fetches - stale while revalidate.
                if (withState.isSuccess && argumentsKey === argumentsDependency) {
                    queryCache.setLastResult(cacheKey, withState);
                }
                settle(withState);
            } catch (error) {
                // An abort means this hook's request was superseded. The superseding request is not
                // necessarily this hook's own: the query instance is shared through the cache, so a
                // co-subscriber mounting with the same query and arguments aborts whatever that shared
                // instance had in flight, and only that co-subscriber's state receives the newer result.
                // The superseded request therefore adopts the outcome of the request that superseded it -
                // otherwise a co-subscriber that never issued the newer request settles on its initial,
                // never-ready state. When this hook superseded itself, that outcome is its own newer one.
                if (isAbortError(error)) {
                    const superseding = await settleFromSupersedingRequest<TDataType>(queryInstance, inFlight);
                    if (superseding === undefined) {
                        settle(current => QueryResultWithState.fromQueryResult(current, false));
                    } else if (superseding.result !== undefined) {
                        settle(QueryResultWithState.fromQueryResult(superseding.result, false));
                    } else {
                        settle(failedResult(queryInstance.defaultValue, superseding.error));
                    }
                    return;
                }
                settle(failedResult(queryInstance.defaultValue, error));
            } finally {
                execution.complete();
                if (executionRef.current === execution) {
                    executionRef.current = undefined;
                }
            }
        }
    });

    // Unmounting retires whatever execution is outstanding, so nothing it is still waiting on can settle the
    // unmounted hook or keep its query scope performing.
    useEffect(() => () => {
        executionRef.current?.retire();
        executionRef.current = undefined;
    }, []);

    useEffect(() => {
        const key = cacheKey;

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
    }, [argumentsDependency, currentPaging, currentSorting, isEnabled]);

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
