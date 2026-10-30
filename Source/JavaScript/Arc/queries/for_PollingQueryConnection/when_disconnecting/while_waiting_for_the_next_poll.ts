// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given.js';
import { a_polling_query_connection } from '../given/a_polling_query_connection.js';

describe('when disconnecting while waiting for the next poll', given(a_polling_query_connection, context => {
    beforeEach(async () => {
        context.connection.connect(context.dataReceived);
        await context.clock.tickAsync(0);
        context.connection.disconnect();
        context.connection.connect(context.dataReceived);
        await context.clock.tickAsync(60000);
    });
    it('should not poll again even if connect is called', () => context.fetchStub.callCount.should.equal(1));
    it('should clear the timer', () => context.clock.countTimers().should.equal(0));
}));
