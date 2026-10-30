// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given.js';
import { a_polling_query_connection } from '../given/a_polling_query_connection.js';

describe('when polling and only the correlation id changes', given(a_polling_query_connection, context => {
    beforeEach(async () => {
        const first = context.envelope({ name: 'same', value: 1 });
        const second = { ...context.envelope({ value: 1, name: 'same' }) };
        context.responses = [new Response(JSON.stringify(first)), new Response(JSON.stringify(second))];
        context.dataReceived.callsFake(result => {
            result.isReady = true;
            result.paging.totalItems = 999;
        });
        context.connection.connect(context.dataReceived);
        await context.clock.tickAsync(0);
        await context.clock.tickAsync(1000);
    });
    it('should ignore property order, correlation id and consumer mutation when comparing', () => {
        context.fetchStub.callCount.should.equal(2);
        context.dataReceived.callCount.should.equal(1);
    });
}));
