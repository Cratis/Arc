// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SortDirection } from './SortDirection.js';

/**
 * Represents sorting for a query.
 */
export class Sorting {

    /**
     * No sorting.
     */
    static none: Sorting = new Sorting('', SortDirection.ascending);

    /**
     * Initializes a new instance of the {@link Sorting} class.
     * @param {string} field The field to sort by.
     * @param {SortDirection} direction The {@link SortDirection} to sort by.
     */
    constructor(readonly field: string, readonly direction: SortDirection) {
    }

    /**
     * Gets whether sorting is enabled.
     */
    get hasSorting(): boolean {
        return this.field !== '';
    }
}