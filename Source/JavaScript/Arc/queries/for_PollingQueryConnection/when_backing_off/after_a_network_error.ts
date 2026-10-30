// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given.js';
import { a_polling_query_connection } from '../given/a_polling_query_connection.js';

describe('when backing off after a network error', given(a_polling_query_connection, context => {
    let beforeRetry: number;
    beforeEach(async () => {
        context.fetchStub.onFirstCall().rejects(new TypeError('offline'));
        context.connection.connect(context.dataReceived);
        await context.clock.tickAsync(1999);
        beforeRetry = context.fetchStub.callCount;
        await context.clock.tickAsync(1001);
    });
    it('should wait twice the interval before retrying', () => beforeRetry.should.equal(1));
    it('should reset to the normal interval after recovery', () => context.fetchStub.callCount.should.equal(3));
    it('should emit the recovered result', () => context.dataReceived.callCount.should.equal(1));
}));
