// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given.js';
import { a_polling_query_connection } from '../given/a_polling_query_connection.js';

describe('when disconnecting and fetch rejects on abort', given(a_polling_query_connection, context => {
    beforeEach(async () => {
        context.fetchStub.callsFake((_url: string, init: RequestInit) => new Promise<Response>((_resolve, reject) => {
            init.signal!.addEventListener('abort', () => reject(new DOMException('The operation was aborted.', 'AbortError')), { once: true });
        }));
        context.connection.connect(context.dataReceived);
        context.connection.disconnect();
        await context.clock.tickAsync(60000);
    });
    it('should not deliver a result', () => context.dataReceived.called.should.be.false);
    it('should not retry the aborted request', () => context.fetchStub.callCount.should.equal(1));
    it('should leave no pending polling timer', () => context.clock.countTimers().should.equal(0));
}));
