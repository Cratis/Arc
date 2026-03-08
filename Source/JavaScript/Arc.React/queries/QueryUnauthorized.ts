// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * The exception that is thrown when a query is not authorized to execute.
 */
export class QueryUnauthorized extends Error {
    constructor() {
        super('Query is not authorized');
        this.name = 'QueryUnauthorized';
        Object.setPrototypeOf(this, QueryUnauthorized.prototype);
    }
}
