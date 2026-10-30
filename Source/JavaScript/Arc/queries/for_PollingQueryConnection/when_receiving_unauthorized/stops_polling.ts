// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given.js';
import { a_polling_query_connection } from '../given/a_polling_query_connection.js';

for (const status of [200, 401, 403]) {
    describe(`when receiving an unauthorized envelope with status ${status}`, given(a_polling_query_connection, context => {
        beforeEach(async () => {
            context.respond(status, {}, { isAuthorized: false });
            context.connection.connect(context.dataReceived);
            await context.clock.tickAsync(60000);
        });
        it('should deliver the unauthorized result', () => context.dataReceived.firstCall.args[0].isAuthorized.should.be.false);
        it('should stop polling', () => context.fetchStub.callCount.should.equal(1));
    }));
}
