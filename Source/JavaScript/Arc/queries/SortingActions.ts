// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SortDirection } from './SortDirection.js';
import { Sorting } from './Sorting.js';

/**
 * Represents sorting for a query.
 */
export class SortingActions {
    private readonly _ascending: Sorting;
    private readonly _descending: Sorting;

    /**
     * Initializes a new instance of {@link SortingActions}.
     * @param {string} field The field that the sorting represents.
     */
    constructor(readonly field: string) { 
        this._ascending = new Sorting(this.field, SortDirection.ascending);
        this._descending = new Sorting(this.field, SortDirection.descending);
    }

    /**
      * Returns ascending sort for the field.
      */
    get ascending(): Sorting {
        return this._ascending;
    }

    /**
      * Returns ascending sort for the field.
      */
    get descending(): Sorting {
        return this._descending;
    }
}
