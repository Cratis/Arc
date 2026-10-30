// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given.js';
import { Globals } from '../../../Globals.js';
import { a_polling_query_connection } from '../given/a_polling_query_connection.js';

describe('when polling with global and instance headers', given(a_polling_query_connection, context => {
    let originalHeadersCallback: typeof Globals.httpHeadersCallback;
    beforeEach(async () => {
        originalHeadersCallback = Globals.httpHeadersCallback;
        Globals.httpHeadersCallback = () => ({ 'X-Global': 'global', Authorization: 'Bearer global' });
        context.connection.connect(context.dataReceived);
        await context.clock.tickAsync(0);
        await context.clock.tickAsync(1000);
    });
    afterEach(() => {
        Globals.httpHeadersCallback = originalHeadersCallback;
    });
    it('should include global headers on every poll', () => {
        context.fetchStub.firstCall.args[1].headers.get('X-Global').should.equal('global');
        context.fetchStub.secondCall.args[1].headers.get('X-Global').should.equal('global');
    });
    it('should let instance headers override the same global header', () => {
        context.fetchStub.firstCall.args[1].headers.get('Authorization').should.equal('Bearer x');
        context.fetchStub.secondCall.args[1].headers.get('Authorization').should.equal('Bearer x');
    });
}));
