// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * The exception that is thrown when a query fails with one or more exceptions on the server.
 */
export class QueryFailed extends Error {
    /**
     * Creates a new instance of {@link QueryFailed}.
     * @param exceptionMessages The exception messages from the server.
     * @param exceptionStackTrace The exception stack trace from the server.
     */
    constructor(
        readonly exceptionMessages: string[],
        readonly exceptionStackTrace: string
    ) {
        super(exceptionMessages.join('\n'));
        this.name = 'QueryFailed';
        Object.setPrototypeOf(this, QueryFailed.prototype);
    }
}
