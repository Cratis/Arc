// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { deepEqual } from '../deepEqual';

/**
 * Resolves the conventional identity of an item so it can be matched against its previous version.
 *
 * Uses the `id` property, the same strategy the server-side change-set computation applies, and
 * reduces concept-style identities (objects with a meaningful `toString()`) to a comparable value.
 * @param {unknown} item The item to resolve identity for.
 * @returns {unknown} The identity value, or `undefined` when the item carries no usable identity.
 */
function getIdentity(item: unknown): unknown {
    const id = (item as Record<string, unknown>)?.id;

    if (id === null || id === undefined) {
        return undefined;
    }

    if (typeof id === 'object') {
        const stringValue = id.toString();
        return stringValue !== '[object Object]' ? stringValue : JSON.stringify(id);
    }

    return id;
}

/**
 * Reconciles a full payload against the previous one, carrying over the previous object references
 * for every item that did not actually change.
 *
 * Observable queries re-deliver a complete snapshot whenever a subscription is (re-)established.
 * Deserializing that snapshot produces entirely new object references, so consumers that compare by
 * reference — memoized components, effect dependencies — treat every item as changed even when the
 * payload is identical to what they already display. Reconciling restores referential stability:
 * unchanged items keep their previous reference, and when nothing changed at all the previous
 * payload itself is returned so callers can detect "no change" with a single `===`.
 *
 * Items are matched by their `id` when present, falling back to position for identity-less items.
 * @template T The payload type.
 * @param {T} previous The payload currently held.
 * @param {T} next The freshly received payload.
 * @returns {T} `previous` when nothing changed; otherwise the new payload with unchanged items carried over by reference.
 */
export function reconcileQueryData<T>(previous: T, next: T): T {
    if (previous === next) {
        return previous;
    }

    if (Array.isArray(previous) && Array.isArray(next)) {
        return reconcileArray(previous, next) as T;
    }

    return deepEqual(previous, next) ? previous : next;
}

/**
 * Reconciles two arrays element by element, preserving previous references for unchanged items.
 * @param {unknown[]} previous The array currently held.
 * @param {unknown[]} next The freshly received array.
 * @returns {unknown[]} `previous` when the arrays are equivalent; otherwise a new array reusing unchanged items.
 */
function reconcileArray(previous: unknown[], next: unknown[]): unknown[] {
    const previousByIdentity = new Map<unknown, unknown>();

    for (const item of previous) {
        const identity = getIdentity(item);

        // Keep the first occurrence — duplicate identities are not a shape we can reconcile
        // meaningfully, and preferring the first keeps the result deterministic.
        if (identity !== undefined && !previousByIdentity.has(identity)) {
            previousByIdentity.set(identity, item);
        }
    }

    let changed = previous.length !== next.length;

    const reconciled = next.map((item, index) => {
        const identity = getIdentity(item);
        const candidate = identity !== undefined ? previousByIdentity.get(identity) : previous[index];

        if (candidate !== undefined && deepEqual(candidate, item)) {
            // An unchanged item that moved still leaves the collection changed as a whole, but the
            // item itself keeps its reference so only the ordering re-renders, not its content.
            if (candidate !== previous[index]) {
                changed = true;
            }

            return candidate;
        }

        changed = true;
        return item;
    });

    return changed ? reconciled : previous;
}
