// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IQueryFor } from './IQueryFor.js';
import { SortDirection } from './SortDirection.js';
import { Sorting } from './Sorting.js';

/**
 * Represents sorting for a query.
 */
export class SortingActionsForQuery<TDataType, TArguments = object> {

    /**
     * Initializes a new instance of {@link SortingActionsForQuery}.
     * @param {string} field The field that the sorting represents.
     * @param {IQueryFor<TDataType, TArguments>} query The query that holds the field.
     */
    constructor(readonly field: string, readonly query: IQueryFor<TDataType, TArguments>) { 
    }

    /**
      * Instructs query to sort ascending for the field.
      */
    ascending(): Sorting {
        this.query.sorting = new Sorting(this.field, SortDirection.ascending);
        return this.query.sorting;
    }

    /**
      * Instructs query to sort ascending for the field.
      */
    descending(): Sorting {
        this.query.sorting = new Sorting(this.field, SortDirection.descending);
        return this.query.sorting;
    }
}
