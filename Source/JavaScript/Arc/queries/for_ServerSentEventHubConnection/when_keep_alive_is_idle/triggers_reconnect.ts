// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_server_sent_event_hub_connection } from '../given/a_server_sent_event_hub_connection';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';

const KEEP_ALIVE_MS = 500;

describe('when keep-alive interval elapses without any server message', given(a_server_sent_event_hub_connection, context => {
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
            KEEP_ALIVE_MS,
            15000,
            context.policy
        );

        context.connection.subscribe('q1', { queryName: 'MyQuery' }, sinon.stub());
        context.simulateOpen();

        // Deliver Connected so the connection is fully established.
        context.simulateMessage({ type: HubMessageType.Connected, payload: 'conn-123' });

        // Advance past the keep-alive interval without any further messages.
        clock.tick(KEEP_ALIVE_MS + 1);
    });

    afterEach(() => {
        clock.restore();
        sinon.restore();
    });

    it('should schedule a reconnect via the policy', () => {
        (context.policy.schedule as sinon.SinonStub).calledOnce.should.be.true;
    });
}));
