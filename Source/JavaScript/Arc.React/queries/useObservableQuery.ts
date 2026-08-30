// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import {
    QueryResultWithState,
    type QueryResult,
    type IObservableQueryFor,
    Sorting,
    Paging,
    type ChangeSet,
    isPrimitiveModelType,
} from '@cratis/arc/queries';
import { type Constructor, JsonSerializer } from '@cratis/fundamentals';
import { useState, useEffect, useContext, useRef, useMemo } from 'react';
import type { SetSorting } from './SetSorting';
import type { SetPage } from './SetPage';
import type { SetPageSize } from './SetPageSize';
import { ArcContext } from '../ArcContext';
import { QueryInstanceCacheContext } from './QueryInstanceCacheContext';
import { serializeArgsForDependency } from './serializeArgsForDependency';
import { useQueryScope } from './useQueryScope';

/**
 * Applies a server-provided {@link ChangeSet} to a snapshot array, producing the new state.
 *
 * Items are matched by the conventional `id` property when present (same strategy as the
 * server-side {@code ChangeSetComputor}). Without an identity property, JSON-string equality
 * is used as a fallback (additions and removals only — no replacements).
 */
type ItemIdentity =
    | string
    | number
    | boolean
    | bigint
    | symbol
    | object
    | null
    | undefined;

function applyChangeSet<T>(previous: T[], changeSet: ChangeSet<unknown>): T[] {
    const getId = (item: unknown): ItemIdentity =>
        (item as Record<string, ItemIdentity>)?.id;
    const toIdentityValue = (id: ItemIdentity): ItemIdentity => {
        if (id === null || id === undefined) {
            return id;
        }

        if (typeof id === 'object') {
            const stringValue = id.toString();
            if (stringValue !== '[object Object]') {
                return stringValue;
            }
            return JSON.stringify(id);
        }

        return id;
    };

    const idsEqual = (left: ItemIdentity, right: ItemIdentity): boolean => {
        if (left === right) {
            return true;
        }

        if (
            left === null ||
            left === undefined ||
            right === null ||
            right === undefined
        ) {
            return false;
        }

        const leftWithEquals = left as { equals?: (other: unknown) => boolean };
        if (typeof leftWithEquals.equals === 'function') {
            return leftWithEquals.equals(right);
        }

        const rightWithEquals = right as { equals?: (other: unknown) => boolean };
        if (typeof rightWithEquals.equals === 'function') {
            return rightWithEquals.equals(left);
        }

        return toIdentityValue(left) === toIdentityValue(right);
    };

    const useIdentity =
        changeSet.removed.length > 0
            ? getId(changeSet.removed[0]) !== undefined
            : changeSet.replaced.length > 0;

    let result: unknown[];

    if (useIdentity) {
        const removedIds = changeSet.removed.map(getId);
        result = (previous as unknown[]).filter(
            (item) => !removedIds.some((removedId) => idsEqual(getId(item), removedId)),
        );

        result = result.map((item) => {
            const replacement = changeSet.replaced.find((candidate) =>
                idsEqual(getId(candidate), getId(item)),
            );
            return replacement === undefined ? item : replacement;
        });
    } else {
        const removedJsons = new Set(
            changeSet.removed.map((item) => JSON.stringify(item)),
        );
        result = (previous as unknown[]).filter(
            (item) => !removedJsons.has(JSON.stringify(item)),
        );
    }

    return [...result, ...changeSet.added] as T[];
}

/**
 * Deserializes a payload collection into its model type, passing primitives through untouched.
 *
 * A query whose backend returns a primitive collection - `IEnumerable<string>`, `IEnumerable<int>` -
 * generates a proxy with `String`, `Number` or `Boolean` as its model type, and
 * {@link JsonSerializer.deserializeArrayFromInstance} is destructive for those: it constructs
 * `new String()` per item and copies declared fields onto it, discarding the value and leaving an
 * empty wrapper object behind.
 *
 * Only the primitive check itself is shared with `@cratis/arc` - the deserialization must run
 * through this package's own {@link JsonSerializer}, whose converter registry is module state and
 * therefore only knows the `Guid`, `Date` and concept types registered in this package's copy.
 * @param {Constructor} modelType The instance type of the items to deserialize into.
 * @param {unknown[]} items The items to deserialize.
 * @returns {unknown[]} The deserialized items, or the items unchanged for primitive model types.
 */
function deserializeItems(modelType: Constructor | null, items: unknown[]): unknown[] {
    if (!modelType || modelType === Object || isPrimitiveModelType(modelType)) {
        return Array.from(items);
    }

    return JsonSerializer.deserializeArrayFromInstance(modelType, items);
}

function deserializeChangeSet(
    changeSet: ChangeSet<unknown>,
    modelType: Constructor,
): ChangeSet<unknown> {
    return {
        added: deserializeItems(modelType, changeSet.added ?? []),
        replaced: deserializeItems(modelType, changeSet.replaced ?? []),
        removed: deserializeItems(modelType, changeSet.removed ?? []),
    };
}

function hasAllRequiredArguments(
    requiredRequestParameters: string[],
    args?: Record<string, unknown>,
): boolean {
    if (requiredRequestParameters.length === 0) {
        return true;
    }

    return requiredRequestParameters.every((requiredRequestParameter) => {
        const value = args?.[requiredRequestParameter];
        return value !== undefined && value !== null && value !== '';
    });
}

function useObservableQueryInternal<
    TDataType,
    TQuery extends IObservableQueryFor<TDataType>,
    TArguments = object,
>(
    query: Constructor<TQuery>,
    sorting?: Sorting,
    paging?: Paging,
    args?: TArguments,
    isEnabled?: boolean,
    owner?: string,
): [QueryResultWithState<TDataType>, SetSorting, SetPage, SetPageSize];
function useObservableQueryInternal<
    TDataType,
    TQuery extends IObservableQueryFor<TDataType>,
    TArguments = object,
>(
    query: Constructor<TQuery>,
    sorting?: Sorting,
    paging?: Paging,
    args?: TArguments,
    isEnabled?: boolean,
    owner?: string,
): [QueryResultWithState<TDataType>, SetSorting, SetPage, SetPageSize] {
    const [currentPaging, setCurrentPaging] = useState<Paging>(paging ?? Paging.noPaging);
    const [currentSorting, setCurrentSorting] = useState<Sorting>(
        sorting ?? Sorting.none,
    );
    const arc = useContext(ArcContext);
    const queryCache = useContext(QueryInstanceCacheContext);
    const queryScope = useQueryScope();
    const cacheKeyRef = useRef<string>('');
    const ownerRef = useRef<string | undefined>(owner);
    ownerRef.current = owner;
    const argsDependency = serializeArgsForDependency(args as object | undefined);

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

        const typeName =
            (freshInstance as { queryName?: string }).queryName ?? query.name;
        const key = queryCache.buildKey(typeName, args as object | undefined);
        cacheKeyRef.current = key;

        const { instance, isNew } = queryCache.getOrCreate(key, () => freshInstance);

        if (!isNew) {
            // Update mutable settings on the shared instance
            (instance as TQuery).paging = currentPaging;
            (instance as TQuery).sorting = currentSorting;
        }

        return instance as TQuery;
    }, [
        currentPaging,
        currentSorting,
        arc.microservice,
        arc.apiBasePath,
        arc.origin,
        argsDependency,
    ]);

    const cachedResult = queryCache.getLastResult<TDataType>(cacheKeyRef.current);

    const [result, setResult] = useState<QueryResultWithState<TDataType>>(
        cachedResult ?? QueryResultWithState.initial(queryInstance.defaultValue),
    );

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
        argsDependency,
        currentPaging,
        currentSorting,
        isEnabled,
        hasAllRequiredArgumentsSet,
        arc.microservice,
        arc.apiBasePath,
        arc.origin,
        arc.queryVersion,
    ];

    useEffect(() => {
        const key = cacheKeyRef.current;
        const listener = listenerRef.current!;

        queryCache.acquire(key);

        if (isEnabled === false || !hasAllRequiredArgumentsSet) {
            return () => {
                queryCache.release(key);
            };
        }

        // Register this component's listener so it receives broadcasts from setLastResult.
        queryCache.addListener(key, listener);

        // If the cached result already exists (another subscriber already received data),
        // immediately apply it to this component's state.
        const existing = queryCache.getLastResult<TDataType>(key);
        if (existing) {
            setResult(existing);
        }

        // Only start a subscription if one does not already exist for this cache key.
        if (!queryCache.isSubscribed(key)) {
            let receivedFirstResult = false;
            queryScope.notifyPerformingStarted();

            const subscription = queryInstance.subscribe((response) => {
                let withState: QueryResultWithState<TDataType>;
                // SAFETY: Observable query implementations expose this runtime metadata even though the interface omits it.
                const queryMetadata = queryInstance as unknown as {
                    modelType?: Constructor;
                    enumerable: boolean;
                    queryName?: string;
                };
                const modelType = queryMetadata.modelType ?? null;

                const responseData: unknown = response.data;
                const isDataArray = Array.isArray(responseData);
                const isEnumerable = queryMetadata.enumerable;
                if (
                    isEnumerable &&
                    !isDataArray &&
                    responseData !== null &&
                    responseData !== undefined
                ) {
                    const responseDataConstructor =
                        typeof responseData === 'object'
                            ? (responseData as { constructor?: { name?: string } })
                                  .constructor?.name
                            : undefined;
                    console.error(
                        `[useObservableQuery] NON-ARRAY data received for key="${key}" queryName="${queryMetadata.queryName}" data type=${typeof responseData} constructor=${responseDataConstructor}`,
                        responseData,
                    );
                }

                if (
                    response.changeSet &&
                    Array.isArray(response.data) &&
                    response.data.length === 0
                ) {
                    // Delta mode subsequent push: the server omits `data` (serialised as null → []).
                    // Reconstruct the full collection by applying the ChangeSet to the previous state.
                    const previousResult = queryCache.getLastResult<TDataType>(key);
                    if (previousResult && Array.isArray(previousResult.data)) {
                        const deserializedChangeSet = deserializeChangeSet(
                            response.changeSet,
                            modelType ?? Object,
                        );
                        const reconstructed = applyChangeSet(
                            previousResult.data as unknown[],
                            deserializedChangeSet,
                        ) as TDataType;
                        withState = QueryResultWithState.fromQueryResult(
                            {
                                ...response,
                                data: reconstructed,
                                changeSet: deserializedChangeSet,
                            } as QueryResult<TDataType>,
                            false,
                        );
                    } else {
                        // Fallback if there's no previous result. ObservableQueryFor has already deserialized response.data.
                        withState = QueryResultWithState.fromQueryResult(response, false);
                    }
                } else {
                    // ObservableQueryFor deserializes initial and full response data before invoking this callback.
                    withState = QueryResultWithState.fromQueryResult(response, false);
                }

                if (!receivedFirstResult) {
                    receivedFirstResult = true;
                    queryScope.notifyPerformingCompleted();
                }

                queryCache.setLastResult(key, withState);
            }, args as object);

            queryCache.setTeardown(key, () => {
                subscription.unsubscribe();
                if (!receivedFirstResult) {
                    receivedFirstResult = true;
                    queryScope.notifyPerformingCompleted();
                }
            });
        }

        arc.observableQueryDiagnostics?.beginTracking(key, ownerRef.current ?? '');
        return () => {
            arc.observableQueryDiagnostics?.endTracking(key);
            queryCache.removeListener(key, listener);
            queryCache.release(key);
        };
    }, effectDeps);

    return [
        isEnabled === false
            ? QueryResultWithState.empty(queryInstance.defaultValue)
            : result,
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
    >(query, sorting, Paging.noPaging, args, isEnabled, owner);
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
    return useObservableQueryInternal<TDataType, TQuery, TArguments>(
        query,
        sorting,
        paging,
        args,
        isEnabled,
        owner,
    );
}
