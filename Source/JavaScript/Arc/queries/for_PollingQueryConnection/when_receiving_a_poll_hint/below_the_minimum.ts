// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given.js';
import { a_polling_query_connection } from '../given/a_polling_query_connection.js';

for (const hint of ['0', '0.1']) {
    describe(`when receiving a poll hint below the minimum: ${hint}`, given(a_polling_query_connection, context => {
        let beforeNextPoll: number;
        beforeEach(async () => {
            context.respond(200, { 'Cratis-Query-Poll-After': hint });
            context.connection.connect(context.dataReceived);
            await context.clock.tickAsync(0);
            await context.clock.tickAsync(499);
            beforeNextPoll = context.fetchStub.callCount;
            await context.clock.tickAsync(1);
        });
        it('should not poll before the minimum', () => beforeNextPoll.should.equal(1));
        it('should poll at the minimum', () => context.fetchStub.callCount.should.equal(2));
    }));
}
