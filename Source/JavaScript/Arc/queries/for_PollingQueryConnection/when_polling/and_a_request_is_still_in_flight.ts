// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given.js';
import { a_polling_query_connection } from '../given/a_polling_query_connection.js';

describe('when polling and a request is still in flight', given(a_polling_query_connection, context => {
    beforeEach(async () => {
        context.fetchStub.returns(new Promise<Response>(() => {}));
        context.connection.connect(context.dataReceived);
        context.connection.connect(context.dataReceived);
        await context.clock.tickAsync(5000);
    });
    it('should have only one request in flight', () => context.fetchStub.callCount.should.equal(1));
}));
