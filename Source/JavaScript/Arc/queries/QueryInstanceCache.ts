// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResultWithState } from './QueryResultWithState';

/**
 * Represents a key that uniquely identifies a query instance in the cache, based on the query type name and its serialized arguments.
 */
export type QueryCacheKey = string;

/**
 * Callback invoked when the cached result for an entry changes.
 */
export type QueryCacheListener<TDataType> = (result: QueryResultWithState<TDataType>) => void;

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

    /**
     * Set of listener callbacks that are notified when {@link lastResult} changes.
     */
    readonly listeners: Set<QueryCacheListener<TDataType>>;

    /**
     * Cleanup function returned by the first subscriber that starts the query connection.
     * Called when the last subscriber releases the entry.
     */
    teardown?: () => void;

    /**
     * Whether an active subscription has been established for this entry.
     */
    subscribed: boolean;

    /**
     * Timer handle for deferred cleanup. Allows React StrictMode re-mounts (in any build
     * environment) to cancel the pending teardown so the connection is reused instead of
     * torn down and recreated.
     */
    pendingCleanup?: ReturnType<typeof setTimeout>;
}

/**
 * Provides a cache for query instances, keyed by query type and serialized arguments.
 *
 * Two callers requesting the same query type with identical arguments receive the same
 * cached instance and immediately see the last known result — without an additional
 * round trip to the server. When the last subscriber releases its reference the entry
 * is evicted.
 */
export class QueryInstanceCache {
    private readonly _entries = new Map<QueryCacheKey, QueryCacheEntry<unknown>>();
    private _pendingDispose?: ReturnType<typeof setTimeout>;

    /**
     * Initializes a new instance of {@link QueryInstanceCache}.
     * @param development Accepted for API compatibility. No longer changes teardown behavior —
     *   teardown is always deferred to handle React StrictMode re-mounts in any environment.
     */
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    constructor(development: boolean = false) {
        // The development parameter is kept for API compatibility only.
        // Teardown is always deferred regardless of this flag.
    }

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
                subscriberCount: 0,
                listeners: new Set(),
                subscribed: false,
            };

            this._entries.set(key, entry);
            return { instance: entry.instance as TInstance, isNew: true };
        }

        const entry = this._entries.get(key)!;
        return { instance: entry.instance as TInstance, isNew: false };
    }

    /**
     * Increments the active subscriber count for the given key.
     * If a deferred cleanup was pending (from a recent {@link release}),
     * it is cancelled so the existing subscription is reused.
     * Call from `useEffect` setup to pair with {@link release} in the cleanup.
     * @param key The cache key produced by {@link buildKey}.
     */
    acquire(key: QueryCacheKey): void {
        const entry = this._entries.get(key);

        if (entry) {
            if (entry.pendingCleanup !== undefined) {
                clearTimeout(entry.pendingCleanup);
                entry.pendingCleanup = undefined;
            }

            entry.subscriberCount++;
        }
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
     * Stores the most recent result for the given key and notifies all registered listeners.
     * @template TDataType The type of data returned by the query.
     * @param key The cache key produced by {@link buildKey}.
     * @param result The result to store.
     */
    setLastResult<TDataType>(key: QueryCacheKey, result: QueryResultWithState<TDataType>): void {
        const entry = this._entries.get(key);

        if (entry) {
            entry.lastResult = result as QueryResultWithState<unknown>;

            for (const listener of entry.listeners) {
                (listener as QueryCacheListener<TDataType>)(result);
            }
        }
    }

    /**
     * Registers a listener that is invoked whenever the cached result for the given key changes.
     * @template TDataType The type of data returned by the query.
     * @param key The cache key produced by {@link buildKey}.
     * @param listener The callback to register.
     */
    addListener<TDataType>(key: QueryCacheKey, listener: QueryCacheListener<TDataType>): void {
        const entry = this._entries.get(key);

        if (entry) {
            entry.listeners.add(listener as QueryCacheListener<unknown>);
        }
    }

    /**
     * Removes a previously registered listener.
     * @template TDataType The type of data returned by the query.
     * @param key The cache key produced by {@link buildKey}.
     * @param listener The callback to remove.
     */
    removeListener<TDataType>(key: QueryCacheKey, listener: QueryCacheListener<TDataType>): void {
        const entry = this._entries.get(key);

        if (entry) {
            entry.listeners.delete(listener as QueryCacheListener<unknown>);
        }
    }

    /**
     * Stores a teardown function for the given key and marks the entry as subscribed.
     * Called automatically when the last subscriber releases the entry.
     * @param key The cache key produced by {@link buildKey}.
     * @param teardown Cleanup function that disconnects the underlying query subscription.
     */
    setTeardown(key: QueryCacheKey, teardown: () => void): void {
        const entry = this._entries.get(key);

        if (entry) {
            entry.teardown = teardown;
            entry.subscribed = true;
        }
    }

    /**
     * Returns whether an active subscription exists for the given key.
     * @param key The cache key to check.
     * @returns `true` if a subscription has been established; `false` otherwise.
     */
    isSubscribed(key: QueryCacheKey): boolean {
        return this._entries.get(key)?.subscribed ?? false;
    }

    /**
     * Decrements the subscriber count for the given key. When the count reaches zero, both teardown
     * and eviction are deferred by one microtask so that React StrictMode re-mounts — in any build
     * environment — can re-acquire the entry and cancel the cleanup before the timeout fires. This
     * prevents an unnecessary disconnect/reconnect cycle during the synthetic unmount/remount that
     * StrictMode performs.
     * @param key The cache key produced by {@link buildKey}.
     */
    release(key: QueryCacheKey): void {
        const entry = this._entries.get(key);

        if (entry) {
            entry.subscriberCount--;

            if (entry.subscriberCount <= 0) {
                // Defer both teardown and deletion so React StrictMode re-mounts in any environment
                // can cancel by calling acquire() before the timeout fires.
                entry.pendingCleanup = setTimeout(() => {
                    const current = this._entries.get(key);

                    if (current && current.subscriberCount <= 0) {
                        current.subscribed = false;
                        current.teardown?.();
                        current.teardown = undefined;
                        current.pendingCleanup = undefined;
                        this._entries.delete(key);
                    }
                }, 0);
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

    /**
     * Tears down all active subscriptions and marks every entry as not subscribed,
     * but preserves entries, subscriber counts, and listeners. This allows
     * hooks whose effects re-run afterward to detect that the entry is no longer
     * subscribed and re-establish a fresh connection.
     *
     * Use this when the underlying transport must be replaced (e.g. after an
     * authentication change that requires new WebSocket connections with updated
     * credentials).
     */
    teardownAllSubscriptions(): void {
        for (const [, entry] of this._entries) {
            if (entry.pendingCleanup !== undefined) {
                clearTimeout(entry.pendingCleanup);
                entry.pendingCleanup = undefined;
            }

            entry.subscribed = false;
            entry.teardown?.();
            entry.teardown = undefined;
        }
    }

    /**
     * Immediately tears down all subscriptions, cancels any pending deferred cleanups,
     * and evicts all entries. Call when the owning component (e.g. the {@link Arc} provider)
     * unmounts permanently so that all query connections are closed synchronously.
     */
    dispose(): void {
        for (const [, entry] of this._entries) {
            if (entry.pendingCleanup !== undefined) {
                clearTimeout(entry.pendingCleanup);
                entry.pendingCleanup = undefined;
            }

            entry.subscribed = false;
            entry.teardown?.();
            entry.teardown = undefined;
        }

        this._entries.clear();
    }

    /**
     * Schedules a deferred {@link dispose} using {@code setTimeout(0)}.
     *
     * This allows React StrictMode re-mounts to call {@link cancelPendingDispose}
     * before the dispose fires, avoiding the destruction of cache entries that child
     * effects are about to re-acquire.
     *
     * If a deferred dispose is already pending, it is replaced.
     */
    deferDispose(): void {
        if (this._pendingDispose !== undefined) {
            clearTimeout(this._pendingDispose);
        }

        this._pendingDispose = setTimeout(() => {
            this._pendingDispose = undefined;
            this.dispose();
        }, 0);
    }

    /**
     * Cancels a pending deferred dispose scheduled by {@link deferDispose}.
     *
     * Call from the {@code useEffect} setup phase so that a StrictMode re-mount
     * prevents the synthetic unmount's deferred dispose from firing.
     */
    cancelPendingDispose(): void {
        if (this._pendingDispose !== undefined) {
            clearTimeout(this._pendingDispose);
            this._pendingDispose = undefined;
        }
    }
}
