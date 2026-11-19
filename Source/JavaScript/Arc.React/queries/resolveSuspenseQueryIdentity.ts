// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Constructor } from '@cratis/fundamentals';

const identities = new WeakMap<object, string>();
let nextConstructorId = 0;

/**
 * Resolves a query's generated name so minification cannot merge unrelated Suspense resources.
 * Custom queries without a generated name receive a unique identity per constructor instead.
 * The probe instance is constructed only once per constructor, including across Suspense retries.
 * @param query The query constructor.
 * @param enabled Whether the query can run. Disabled hooks must not construct a metadata probe.
 * @returns The stable resource identity, or the unused runtime name while disabled.
 */
export function resolveSuspenseQueryIdentity(query: Constructor<unknown>, enabled = true): string {
    if (!enabled) {
        return (query as unknown as { name: string }).name;
    }

    const existing = identities.get(query);
    if (existing !== undefined) {
        return existing;
    }

    const instance = new query();
    const queryName = (instance as { queryName?: unknown }).queryName;
    const identity = typeof queryName === 'string' && queryName.length > 0
        ? queryName
        : `__custom_query__#${++nextConstructorId}`;
    identities.set(query, identity);
    return identity;
}
