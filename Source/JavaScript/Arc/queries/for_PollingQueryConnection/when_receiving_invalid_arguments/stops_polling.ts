// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given.js';
import { a_polling_query_connection } from '../given/a_polling_query_connection.js';

describe('when receiving invalid arguments', given(a_polling_query_connection, context => {
    beforeEach(async () => {
        context.respond(400, {}, { isValid: false });
        context.connection.connect(context.dataReceived);
        await context.clock.tickAsync(60000);
    });
    it('should deliver the validation result', () => context.dataReceived.firstCall.args[0].isValid.should.be.false);
    it('should stop polling', () => context.fetchStub.callCount.should.equal(1));
}));
