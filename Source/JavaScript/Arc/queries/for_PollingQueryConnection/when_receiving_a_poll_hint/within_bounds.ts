// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given.js';
import { a_polling_query_connection } from '../given/a_polling_query_connection.js';

describe('when receiving a poll hint within bounds', given(a_polling_query_connection, context => {
    let beforeNextPoll: number;
    beforeEach(async () => {
        context.respond(200, { 'Cratis-Query-Poll-After': '3' });
        context.connection.connect(context.dataReceived);
        await context.clock.tickAsync(0);
        await context.clock.tickAsync(2999);
        beforeNextPoll = context.fetchStub.callCount;
        await context.clock.tickAsync(1);
    });
    it('should wait for the hint in seconds', () => beforeNextPoll.should.equal(1));
    it('should poll at the hinted interval', () => context.fetchStub.callCount.should.equal(2));
}));
