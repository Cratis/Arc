// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_server_sent_event_hub_connection } from '../given/a_server_sent_event_hub_connection';
import { given } from '../../../given';

const CONNECT_TIMEOUT_MS = 500;

describe('when the connect timeout elapses before a Connected message arrives', given(a_server_sent_event_hub_connection, context => {
    let clock: sinon.SinonFakeTimers;

    beforeEach(() => {
        clock = sinon.useFakeTimers();
        context.setup();

        const { ServerSentEventHubConnection } = require('../../ServerSentEventHubConnection');
        context.connection = new ServerSentEventHubConnection(
            'http://localhost/.cratis/queries/sse',
            'http://localhost/.cratis/queries/sse/subscribe',
            'http://localhost/.cratis/queries/sse/unsubscribe',
            '',
            30000,
            CONNECT_TIMEOUT_MS,
            context.policy
        );

        context.connection.subscribe('q1', { queryName: 'MyQuery' }, sinon.stub());

        // Open the EventSource but never deliver a Connected message.
        context.simulateOpen();

        // Advance past the connect timeout.
        clock.tick(CONNECT_TIMEOUT_MS + 1);
    });

    afterEach(() => {
        clock.restore();
        sinon.restore();
    });

    it('should schedule a reconnect via the policy', () => {
        (context.policy.schedule as sinon.SinonStub).calledOnce.should.be.true;
    });
}));
