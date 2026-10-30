// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given.js';
import { a_polling_query_connection } from '../given/a_polling_query_connection.js';

describe('when disconnecting while a request is in flight', given(a_polling_query_connection, context => {
    beforeEach(async () => {
        let resolveRequest!: (response: Response) => void;
        context.fetchStub.returns(new Promise<Response>(resolve => { resolveRequest = resolve; }));
        context.connection.connect(context.dataReceived);
        context.connection.disconnect();
        resolveRequest(new Response(JSON.stringify(context.envelope('late'))));
        await context.clock.tickAsync(60000);
    });
    it('should abort the request', () => context.fetchStub.firstCall.args[1].signal.aborted.should.be.true);
    it('should ignore a late result', () => context.dataReceived.called.should.be.false);
    it('should not poll again', () => context.fetchStub.callCount.should.equal(1));
}));
