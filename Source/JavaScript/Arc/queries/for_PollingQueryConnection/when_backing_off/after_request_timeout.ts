// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given.js';
import { a_polling_query_connection } from '../given/a_polling_query_connection.js';

describe('when backing off after request timeout', given(a_polling_query_connection, context => {
    let beforeRetry: number;
    let emissionsBeforeRetry: number;
    beforeEach(async () => {
        context.respond(408);
        context.connection.connect(context.dataReceived);
        await context.clock.tickAsync(0);
        await context.clock.tickAsync(1999);
        beforeRetry = context.fetchStub.callCount;
        emissionsBeforeRetry = context.dataReceived.callCount;
        await context.clock.tickAsync(1001);
    });
    it('should wait twice the interval', () => beforeRetry.should.equal(1));
    it('should not emit the timeout response', () => emissionsBeforeRetry.should.equal(0));
    it('should return to the normal interval after recovery', () => context.fetchStub.callCount.should.equal(3));
}));
