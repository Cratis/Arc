// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given.js';
import { a_polling_query_connection } from '../given/a_polling_query_connection.js';

for (const hint of ['abc', '-1', 'Infinity']) {
    describe(`when receiving a poll hint that is invalid: ${hint}`, given(a_polling_query_connection, context => {
        let beforeNextPoll: number;
        beforeEach(async () => {
            context.respond(200, { 'Cratis-Query-Poll-After': hint });
            context.connection.connect(context.dataReceived);
            await context.clock.tickAsync(0);
            await context.clock.tickAsync(999);
            beforeNextPoll = context.fetchStub.callCount;
            await context.clock.tickAsync(1);
        });
        it('should wait until the configured interval', () => beforeNextPoll.should.equal(1));
        it('should poll at the configured interval', () => context.fetchStub.callCount.should.equal(2));
    }));
}
