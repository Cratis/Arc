// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResultWithState } from './QueryResultWithState';

/**
 * Represents a key that uniquely identifies a query instance in the cache, based on the query type name and its serialised arguments.
 */
export type QueryCacheKey = string;

/**
 * Represents a single entry in the {@link QueryInstanceCache}.
 * @template TDataType The type of data returned by the query.
 */
export interface QueryCacheEntry<TDataType> {
    /**
     * The cached query instance.
     */
    readonly instance: unknown;

    /**
     * The last result received from the query, if any.
     */
    lastResult?: QueryResultWithState<TDataType>;

    /**
     * The number of active subscribers holding a reference to this entry.
     */
    subscriberCount: number;
}

/**
 * Provides a cache for query instances, keyed by query type and serialised arguments.
 *
 * Two callers requesting the same query type with identical arguments receive the same
 * cached instance and immediately see the last known result — without an additional
 * round trip to the server. When the last subscriber releases its reference the entry
 * is evicted.
 */
export class QueryInstanceCache {
    private readonly _entries = new Map<QueryCacheKey, QueryCacheEntry<unknown>>();

    /**
     * Builds the cache key for a query.
     * @param queryTypeName The name of the query constructor (i.e. `constructor.name`).
     * @param args Optional arguments supplied to the query.
     * @returns A stable string key.
     */
    buildKey(queryTypeName: string, args?: object): QueryCacheKey {
        if (!args || Object.keys(args).length === 0) {
            return `${queryTypeName}::`;
        }

        const sorted = Object.keys(args)
            .sort()
            .reduce<Record<string, unknown>>((accumulator, key) => {
                accumulator[key] = (args as Record<string, unknown>)[key];
                return accumulator;
            }, {});

        return `${queryTypeName}::${JSON.stringify(sorted)}`;
    }

    /**
     * Returns a cached instance for the given key, or creates a new one using the provided factory.
     * The subscriber count of the entry is incremented.
     * @template TInstance The type of the query instance.
     * @param key The cache key produced by {@link buildKey}.
     * @param factory A zero-argument factory that creates a fresh query instance when one is not yet cached.
     * @returns The cached (or newly created) instance and whether it was newly created.
     */
    getOrCreate<TInstance>(
        key: QueryCacheKey,
        factory: () => TInstance
    ): { instance: TInstance; isNew: boolean } {
        if (!this._entries.has(key)) {
            const entry: QueryCacheEntry<unknown> = {
                instance: factory(),
                lastResult: undefined,
                subscriberCount: 1,
            };

            this._entries.set(key, entry);
            return { instance: entry.instance as TInstance, isNew: true };
        }

        const entry = this._entries.get(key)!;
        entry.subscriberCount++;
        return { instance: entry.instance as TInstance, isNew: false };
    }

    /**
     * Returns the last cached result for the given key, or `undefined` if no result has been stored yet.
     * @template TDataType The type of data returned by the query.
     * @param key The cache key produced by {@link buildKey}.
     * @returns The last {@link QueryResultWithState}, or `undefined`.
     */
    getLastResult<TDataType>(key: QueryCacheKey): QueryResultWithState<TDataType> | undefined {
        return this._entries.get(key)?.lastResult as QueryResultWithState<TDataType> | undefined;
    }

    /**
     * Stores the most recent result for the given key so that new subscribers can receive it immediately.
     * @template TDataType The type of data returned by the query.
     * @param key The cache key produced by {@link buildKey}.
     * @param result The result to store.
     */
    setLastResult<TDataType>(key: QueryCacheKey, result: QueryResultWithState<TDataType>): void {
        const entry = this._entries.get(key);

        if (entry) {
            entry.lastResult = result as QueryResultWithState<unknown>;
        }
    }

    /**
     * Decrements the subscriber count for the given key. When the count reaches zero the entry is evicted.
     * @param key The cache key produced by {@link buildKey}.
     */
    release(key: QueryCacheKey): void {
        const entry = this._entries.get(key);

        if (entry) {
            entry.subscriberCount--;

            if (entry.subscriberCount <= 0) {
                this._entries.delete(key);
            }
        }
    }

    /**
     * Returns whether an entry exists for the given key.
     * @param key The cache key to check.
     * @returns `true` if an entry exists; `false` otherwise.
     */
    has(key: QueryCacheKey): boolean {
        return this._entries.has(key);
    }
}
