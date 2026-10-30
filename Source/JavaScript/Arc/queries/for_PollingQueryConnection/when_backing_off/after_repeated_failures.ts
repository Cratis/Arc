// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given.js';
import { a_polling_query_connection } from '../given/a_polling_query_connection.js';

describe('when backing off after repeated failures', given(a_polling_query_connection, context => {
    const requestTimes: number[] = [];
    beforeEach(async () => {
        requestTimes.length = 0;
        context.fetchStub.callsFake(async () => {
            requestTimes.push(context.clock.now);
            throw new TypeError('offline');
        });
        context.connection.connect(context.dataReceived);
        await context.clock.tickAsync(22000);
    });
    it('should double the delay up to the maximum', () => requestTimes.should.deep.equal([0, 2000, 6000, 14000, 22000]));
}));
