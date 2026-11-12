// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { type ChangeSet, isPrimitiveModelType } from '@cratis/arc/queries';
import { type Constructor, JsonSerializer } from '@cratis/fundamentals';

type ItemIdentity =
    | string
    | number
    | boolean
    | bigint
    | symbol
    | object
    | null
    | undefined;

type WithEquals = { equals?: (other: unknown) => boolean };

const getId = (item: unknown): ItemIdentity =>
    (item as Record<string, ItemIdentity>)?.id;

function toIdentityValue(id: ItemIdentity): ItemIdentity {
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
}

function isNullish(value: ItemIdentity): value is null | undefined {
    return value === null || value === undefined;
}

function idsEqual(left: ItemIdentity, right: ItemIdentity): boolean {
    if (left === right) {
        return true;
    }

    if (isNullish(left) || isNullish(right)) {
        return false;
    }

    const leftWithEquals = left as WithEquals;
    if (typeof leftWithEquals.equals === 'function') {
        return leftWithEquals.equals(right);
    }

    const rightWithEquals = right as WithEquals;
    if (typeof rightWithEquals.equals === 'function') {
        return rightWithEquals.equals(left);
    }

    return toIdentityValue(left) === toIdentityValue(right);
}

function shouldUseIdentity(changeSet: ChangeSet<unknown>): boolean {
    return changeSet.removed.length > 0
        ? getId(changeSet.removed[0]) !== undefined
        : changeSet.replaced.length > 0;
}

function applyByIdentity(previous: unknown[], changeSet: ChangeSet<unknown>): unknown[] {
    const removedIds = changeSet.removed.map(getId);
    const remaining = previous.filter(
        (item) => !removedIds.some((removedId) => idsEqual(getId(item), removedId)),
    );

    return remaining.map((item) => {
        const replacement = changeSet.replaced.find((candidate) =>
            idsEqual(getId(candidate), getId(item)),
        );
        return replacement === undefined ? item : replacement;
    });
}

function applyByJson(previous: unknown[], changeSet: ChangeSet<unknown>): unknown[] {
    const removedJsons = new Set(changeSet.removed.map((item) => JSON.stringify(item)));
    return previous.filter((item) => !removedJsons.has(JSON.stringify(item)));
}

/**
 * Applies a server-provided {@link ChangeSet} to a snapshot array, producing the new state.
 *
 * Items are matched by the conventional `id` property when present (same strategy as the
 * server-side {@code ChangeSetComputor}). Without an identity property, JSON-string equality
 * is used as a fallback (additions and removals only — no replacements).
 * @param {unknown[]} previous The previous snapshot.
 * @param {ChangeSet} changeSet The change set to apply.
 * @returns {unknown[]} The new snapshot.
 */
export function applyChangeSet<T>(previous: T[], changeSet: ChangeSet<unknown>): T[] {
    const result = shouldUseIdentity(changeSet)
        ? applyByIdentity(previous as unknown[], changeSet)
        : applyByJson(previous as unknown[], changeSet);

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

/**
 * Deserializes every collection of a {@link ChangeSet} into the given model type.
 * @param {ChangeSet} changeSet The change set to deserialize.
 * @param {Constructor} modelType The instance type of the items.
 * @returns {ChangeSet} The deserialized change set.
 */
export function deserializeChangeSet(
    changeSet: ChangeSet<unknown>,
    modelType: Constructor,
): ChangeSet<unknown> {
    return {
        added: deserializeItems(modelType, changeSet.added ?? []),
        replaced: deserializeItems(modelType, changeSet.replaced ?? []),
        removed: deserializeItems(modelType, changeSet.removed ?? []),
    };
}
