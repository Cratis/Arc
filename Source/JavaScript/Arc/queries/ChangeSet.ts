// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Represents the set of changes in an observable query result, describing
 * which items were added, replaced, or removed since the previous update.
 * @template T The item type in the observable collection.
 */
export interface ChangeSet<T> {
    /**
     * Items that were added since the last update.
     */
    readonly added: T[];

    /**
     * Items that were replaced (same identity, updated content) since the last update.
     */
    readonly replaced: T[];

    /**
     * Items that were removed since the last update.
     */
    readonly removed: T[];
}
