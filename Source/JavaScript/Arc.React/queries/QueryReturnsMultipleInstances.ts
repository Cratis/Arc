// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * The exception that is thrown when a query used to populate a command's initial values returns
 * multiple instances instead of a single one.
 */
export class QueryReturnsMultipleInstances extends Error {
    /**
     * Creates a new instance of {@link QueryReturnsMultipleInstances}.
     * @param queryName The name of the query type that returns multiple instances.
     */
    constructor(readonly queryName: string) {
        super(`Query '${queryName}' returns multiple instances and cannot be used to populate a command's initial values - it must be a single-instance query`);
        this.name = 'QueryReturnsMultipleInstances';
        Object.setPrototypeOf(this, QueryReturnsMultipleInstances.prototype);
    }
}
