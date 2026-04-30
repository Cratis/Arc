// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResultWithState, IObservableQueryFor, Sorting, Paging, ChangeSet } from '@cratis/arc/queries';
import { Constructor, JsonSerializer } from '@cratis/fundamentals';
import { useState, useEffect, useContext, useRef, useMemo } from 'react';
import { SetSorting } from './SetSorting';
import { SetPage } from './SetPage';
import { SetPageSize } from './SetPageSize';
import { ArcContext } from '../ArcContext';
import { QueryInstanceCacheContext } from './QueryInstanceCacheContext';

/**
 * Applies a server-provided {@link ChangeSet} to a snapshot array, producing the new state.
 *
 * Items are matched by the conventional `id` property when present (same strategy as the
 * server-side {@code ChangeSetComputor}). Without an identity property, JSON-string equality
 * is used as a fallback (additions and removals only — no replacements).
 */
function applyChangeSet<T>(previous: T[], changeSet: ChangeSet<unknown>): T[] {
    const getId = (item: unknown): unknown => (item as Record<string, unknown>)?.id;
    const toIdentityValue = (id: unknown): unknown => {
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

    const idsEqual = (left: unknown, right: unknown): boolean => {
        if (left === right) {
            return true;
        }

        if (left === null || left === undefined || right === null || right === undefined) {
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

    const useIdentity = changeSet.removed.length > 0
        ? getId(changeSet.removed[0]) !== undefined
        : changeSet.replaced.length > 0;

    let result: unknown[];

    if (useIdentity) {
        const removedIds = changeSet.removed.map(getId);
        result = (previous as unknown[]).filter(item => !removedIds.some(removedId => idsEqual(getId(item), removedId)));

        result = result.map(item => {
            const replacement = changeSet.replaced.find(candidate => idsEqual(getId(candidate), getId(item)));
            return replacement !== undefined ? replacement : item;
        });
    } else {
        const removedJsons = new Set(changeSet.removed.map(item => JSON.stringify(item)));
        result = (previous as unknown[]).filter(item => !removedJsons.has(JSON.stringify(item)));
    }

    return [...result, ...changeSet.added] as T[];
}

function deserializeChangeSet(changeSet: ChangeSet<unknown>, modelType: Constructor): ChangeSet<unknown> {
    return {
        added: JsonSerializer.deserializeArrayFromInstance(modelType, changeSet.added ?? []),
        replaced: JsonSerializer.deserializeArrayFromInstance(modelType, changeSet.replaced ?? []),
        removed: JsonSerializer.deserializeArrayFromInstance(modelType, changeSet.removed ?? []),
    };
}

function deserializeResponseData<TDataType>(data: unknown, modelType: Constructor | null): TDataType {
    // If data is an array and we have a model type, deserialize each item
    if (Array.isArray(data) && modelType && modelType !== Object) {
        return JsonSerializer.deserializeArrayFromInstance(modelType, data) as TDataType;
    }
    // Otherwise return data as-is (could be null, undefined, or non-array type)
    return data as TDataType;
}

function hasAllRequiredArguments(requiredRequestParameters: string[], args?: object): boolean {
    if (requiredRequestParameters.length === 0) {
        return true;
    }

    const argumentValues = args as Record<string, unknown> | undefined;
    return requiredRequestParameters.every(requiredRequestParameter => {
        const value = argumentValues?.[requiredRequestParameter];
        return value !== undefined && value !== null && value !== '';
    });
}

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

    // Stable listener ref so we can add/remove the same function reference.
    const listenerRef = useRef<(r: QueryResultWithState<TDataType>) => void>();
    if (!listenerRef.current) {
        listenerRef.current = (r: QueryResultWithState<TDataType>) => setResult(r);
    }

    const hasAllRequiredArgumentsSet = hasAllRequiredArguments(queryInstance.requiredRequestParameters, args as object | undefined);

    // Use all arg values (not just required ones) because the cache key includes every arg.
    // Also include arc context values so the effect re-runs and cleans up the old subscription
    // when the microservice, API base path, or origin changes.
    // Include queryVersion so that reconnectQueries() forces all hooks to re-subscribe
    // through fresh transport connections.
    const effectDeps = [...(args ? Object.values(args) : []), currentPaging, currentSorting, isEnabled, hasAllRequiredArgumentsSet, arc.microservice, arc.apiBasePath, arc.origin, arc.queryVersion];

    useEffect(() => {
        const key = cacheKeyRef.current;
        const listener = listenerRef.current!;

        queryCache.acquire(key);

        if (!isEnabled || !hasAllRequiredArgumentsSet) {
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
            const subscription = queryInstance.subscribe(response => {
                let withState: QueryResultWithState<TDataType>;
                const modelType = (queryInstance as unknown as { modelType?: Constructor }).modelType ?? null;

                if (response.changeSet && Array.isArray(response.data) && response.data.length === 0) {
                    // Delta mode subsequent push: the server omits `data` (serialised as null → []).
                    // Reconstruct the full collection by applying the ChangeSet to the previous state.
                    const previousResult = queryCache.getLastResult<TDataType>(key);
                    if (previousResult && Array.isArray(previousResult.data)) {
                        const deserializedChangeSet = deserializeChangeSet(response.changeSet, modelType ?? Object);
                        const reconstructed = applyChangeSet(previousResult.data as unknown[], deserializedChangeSet) as TDataType;
                        withState = new QueryResultWithState<TDataType>(
                            reconstructed,
                            response.paging,
                            response.isSuccess,
                            response.isAuthorized,
                            response.isValid,
                            response.validationResults,
                            response.hasExceptions,
                            response.exceptionMessages,
                            response.exceptionStackTrace,
                            false,
                            deserializedChangeSet
                        );
                    } else {
                        // Fallback if there's no previous result
                        const deserializedData = deserializeResponseData<TDataType>(response.data, modelType);
                        withState = new QueryResultWithState<TDataType>(
                            deserializedData,
                            response.paging,
                            response.isSuccess,
                            response.isAuthorized,
                            response.isValid,
                            response.validationResults,
                            response.hasExceptions,
                            response.exceptionMessages,
                            response.exceptionStackTrace,
                            false,
                            response.changeSet);
                    }
                } else {
                    // Initial response or full-data responses (non-delta mode)
                    const deserializedData = deserializeResponseData<TDataType>(response.data, modelType);
                    withState = new QueryResultWithState<TDataType>(
                        deserializedData,
                        response.paging,
                        response.isSuccess,
                        response.isAuthorized,
                        response.isValid,
                        response.validationResults,
                        response.hasExceptions,
                        response.exceptionMessages,
                        response.exceptionStackTrace,
                        false,
                        response.changeSet);
                }

                queryCache.setLastResult(key, withState);
            }, args as object);

            queryCache.setTeardown(key, () => {
                subscription.unsubscribe();
            });
        }

        return () => {
            queryCache.removeListener(key, listener);
            queryCache.release(key);
        };
    }, effectDeps);

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
