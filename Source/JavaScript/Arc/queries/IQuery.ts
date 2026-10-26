// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ICanBeConfigured } from '../ICanBeConfigured.js';
import { Paging } from './Paging.js';
import { Sorting } from './Sorting.js';
import { QueryHttpMethod } from './QueryHttpMethod.js';
import { SliceLocation } from '../reflection/SliceLocation.js';

/**
 * Defines the commonalities between all query types.
 */
export interface IQuery extends ICanBeConfigured {
    /**
     * Gets the {@link SliceLocation} of the query, as declared by the static `sliceLocation` member of a generated proxy.
     * @remarks
     * Optional so implementations that predate it keep compiling; the Arc base classes always provide it.
     * Use {@link SliceLocation.of} to read it from any instance with a fallback to {@link SliceLocation.empty}.
     */
    readonly sliceLocation?: SliceLocation;

    /**
     * Set the HTTP method used to perform the query, overriding the global default.
     * @param method The {@link QueryHttpMethod} to use.
     */
    setHttpMethod(method: QueryHttpMethod): void;

    /**
     * Gets the sorting for the query.
     */
    get sorting(): Sorting;

    /**
     * Sets the sorting for the query.
     */
    set sorting(value: Sorting);

    /**
     * Gets the paging for the query.
     */
    get paging(): Paging;

    /**
     * Sets the paging for the query.
     */
    set paging(value: Paging);
}
