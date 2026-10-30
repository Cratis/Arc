// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given.js';
import { a_polling_query_connection } from '../given/a_polling_query_connection.js';

describe('when backing off with jitter', given(a_polling_query_connection, context => {
    let beforeRetry: number;
    beforeEach(async () => {
        context.random.returns(1);
        context.fetchStub.onFirstCall().rejects(new TypeError('offline'));
        context.connection.connect(context.dataReceived);
        await context.clock.tickAsync(2399);
        beforeRetry = context.fetchStub.callCount;
        await context.clock.tickAsync(1);
    });
    it('should not retry before the jittered delay', () => beforeRetry.should.equal(1));
    it('should retry at the jittered delay', () => context.fetchStub.callCount.should.equal(2));
}));
