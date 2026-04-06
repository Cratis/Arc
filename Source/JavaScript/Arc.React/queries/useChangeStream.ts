// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IChangeStreamFor, ChangeSet, Sorting, Paging, QueryResultWithState } from '@cratis/arc/queries';
import { Globals, ObservableQueryTransferMode } from '@cratis/arc';
import { Constructor } from '@cratis/fundamentals';
import { useState, useEffect, useRef } from 'react';
import { useObservableQuery, useObservableQueryWithPaging } from './useObservableQuery';

/**
 * Computes the delta between two snapshots of a collection.
 *
 * When a key accessor is provided, items with the same key but different
 * serialized content are reported as {@code replaced}.  Without a key
 * accessor every item is keyed by its full JSON representation so only
 * additions and removals are surfaced.
 *
 * @param previous The previous snapshot.
 * @param current The new snapshot.
 * @param getKey Optional function that extracts a stable identity key from an item.
 * @returns A {@link ChangeSet} describing what changed.
 */
function computeChangeSet<T>(previous: T[], current: T[], getKey?: (item: T) => unknown): ChangeSet<T> {
    if (getKey) {
        // Key-based delta: we can detect replaced items.
        const previousByKey = new Map<unknown, T>(previous.map(item => [getKey(item), item]));
        const currentByKey = new Map<unknown, T>(current.map(item => [getKey(item), item]));

        const added: T[] = [];
        const replaced: T[] = [];
        const removed: T[] = [];

        for (const [key, item] of currentByKey) {
            if (!previousByKey.has(key)) {
                added.push(item);
            } else if (JSON.stringify(item) !== JSON.stringify(previousByKey.get(key))) {
                replaced.push(item);
            }
        }

        for (const [key, item] of previousByKey) {
            if (!currentByKey.has(key)) {
                removed.push(item);
            }
        }

        return { added, replaced, removed };
    }

    // Fallback: full-JSON keying — cannot detect "replaced" (identity unknown).
    const previousKeys = new Set<string>(previous.map(item => JSON.stringify(item)));
    const currentKeys = new Set<string>(current.map(item => JSON.stringify(item)));

    const added = current.filter(item => !previousKeys.has(JSON.stringify(item)));
    const removed = previous.filter(item => !currentKeys.has(JSON.stringify(item)));

    return { added, replaced: [], removed };
}

function useChangeStreamInternal<TDataType, TQuery extends IChangeStreamFor<TDataType>, TArguments = object>(
    query: Constructor<TQuery>,
    getKey: ((item: TDataType) => unknown) | undefined,
    sorting: Sorting | undefined,
    paging: Paging | undefined,
    args: TArguments | undefined,
    isEnabled: boolean
): ChangeSet<TDataType> {
    const emptyChangeSet: ChangeSet<TDataType> = { added: [], replaced: [], removed: [] };
    const [changeSet, setChangeSet] = useState<ChangeSet<TDataType>>(emptyChangeSet);
    const previousDataRef = useRef<TDataType[]>([]);
    const isFirstUpdateRef = useRef(true);

    // Subscribe via the standard observable query hook (handles caching, auth, lifecycle).
    let result: QueryResultWithState<TDataType[]>;
    if (paging) {
        [result] = useObservableQueryWithPaging<TDataType[], TQuery, TArguments>(query as unknown as Constructor<TQuery>, paging, args, sorting, isEnabled);
    } else {
        [result] = useObservableQuery<TDataType[], TQuery, TArguments>(query as unknown as Constructor<TQuery>, args, sorting, isEnabled);
    }

    useEffect(() => {
        if (!isEnabled || result.isPerforming) {
            return;
        }

        const current: TDataType[] = Array.isArray(result.data) ? result.data : [];
        const previous = previousDataRef.current;

        if (Globals.observableQueryTransferMode === ObservableQueryTransferMode.Full || isFirstUpdateRef.current) {
            // Full mode or first update: treat the whole collection as "added".
            setChangeSet({ added: current, replaced: [], removed: [] });
            isFirstUpdateRef.current = false;
            previousDataRef.current = current;
            return;
        }

        // Delta mode: compute what actually changed.
        const computed = computeChangeSet(previous, current, getKey);
        if (computed.added.length > 0 || computed.replaced.length > 0 || computed.removed.length > 0) {
            setChangeSet(computed);
        }

        previousDataRef.current = current;
    }, [result, isEnabled]);

    return changeSet;
}

/**
 * React hook that subscribes to an observable query and exposes per-update change deltas
 * (added, replaced, removed items) as React state rather than the full collection snapshot.
 *
 * The hook uses the same underlying subscription and caching infrastructure as
 * {@link useObservableQuery} — no additional server connections are opened.
 *
 * @template TDataType The element type of the observable collection.
 * @template TQuery The observable query class (must implement {@link IChangeStreamFor}).
 * @template TArguments Optional query arguments type.
 * @param query The observable query constructor.
 * @param args Optional query arguments.
 * @param getKey Optional function that returns a stable identity key for an item.
 *   When provided, items with the same key but different content are reported as
 *   {@code replaced}. Without it, only additions and removals are detected.
 * @param sorting Optional sorting configuration.
 * @param isEnabled When false the hook is a no-op. Defaults to true.
 * @returns The latest {@link ChangeSet} describing what changed in the most recent update.
 */
export function useChangeStream<TDataType, TQuery extends IChangeStreamFor<TDataType>, TArguments = object>(
    query: Constructor<TQuery>,
    args?: TArguments,
    getKey?: (item: TDataType) => unknown,
    sorting?: Sorting,
    isEnabled: boolean = true
): ChangeSet<TDataType> {
    return useChangeStreamInternal<TDataType, TQuery, TArguments>(query, getKey, sorting, undefined, args, isEnabled);
}
