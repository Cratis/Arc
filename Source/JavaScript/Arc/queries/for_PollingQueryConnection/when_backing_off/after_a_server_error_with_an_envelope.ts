// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given.js';
import { a_polling_query_connection } from '../given/a_polling_query_connection.js';

describe('when backing off after a server error with an envelope', given(a_polling_query_connection, context => {
    let beforeRetry: number;
    beforeEach(async () => {
        context.fetchStub.callsFake(async () => new Response(JSON.stringify(context.envelope(null, { hasExceptions: true })), { status: 500 }));
        context.connection.connect(context.dataReceived);
        await context.clock.tickAsync(0);
        await context.clock.tickAsync(1999);
        beforeRetry = context.fetchStub.callCount;
        await context.clock.tickAsync(1);
    });
    it('should emit the error envelope once', () => {
        context.dataReceived.callCount.should.equal(1);
        context.dataReceived.firstCall.args[0].hasExceptions.should.be.true;
    });
    it('should back off before retrying', () => {
        beforeRetry.should.equal(1);
        context.fetchStub.callCount.should.equal(2);
    });
}));
