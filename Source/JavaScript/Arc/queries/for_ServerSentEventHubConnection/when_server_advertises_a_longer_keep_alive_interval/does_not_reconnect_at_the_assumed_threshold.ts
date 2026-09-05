// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_server_sent_event_hub_connection } from '../given/a_server_sent_event_hub_connection';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';

const ASSUMED_KEEP_ALIVE_MS = 500;
const SERVER_KEEP_ALIVE_MS = 2000;

describe('when the server advertises a longer keep-alive interval than the client assumed', given(a_server_sent_event_hub_connection, context => {
    let clock: sinon.SinonFakeTimers;

    beforeEach(() => {
        clock = sinon.useFakeTimers();
        context.setup();

        // eslint-disable-next-line @typescript-eslint/no-require-imports, @typescript-eslint/no-var-requires
        const { ServerSentEventHubConnection } = require('../../ServerSentEventHubConnection');
        context.connection = new ServerSentEventHubConnection(
            'http://localhost/.cratis/queries/sse',
            'http://localhost/.cratis/queries/sse/subscribe',
            'http://localhost/.cratis/queries/sse/unsubscribe',
            '',
            ASSUMED_KEEP_ALIVE_MS,
            15000,
            context.policy
        );

        context.connection.subscribe('q1', { queryName: 'MyQuery' }, sinon.stub());
        context.simulateOpen();

        context.simulateMessage({
            type: HubMessageType.Connected,
            payload: 'conn-123',
            keepAliveIntervalMs: SERVER_KEEP_ALIVE_MS
        });
    });

    afterEach(() => {
        clock.restore();
        sinon.restore();
    });

    describe('and the assumed threshold elapses', () => {
        beforeEach(() => {
            // Far past the assumed threshold (2 × 500ms), but the server only promised a message
            // every 2000ms — tearing the connection down here is exactly the bug being fixed.
            clock.tick(ASSUMED_KEEP_ALIVE_MS * 2 + 1);
        });

        it('should not schedule a reconnect', () => (context.policy.schedule as sinon.SinonStub).called.should.be.false);
    });

    describe('and the advertised threshold elapses', () => {
        beforeEach(() => {
            // Checks now run on the server's 2000ms cadence; the tick at 4000ms sees 4000ms of silence.
            clock.tick(SERVER_KEEP_ALIVE_MS * 2 + 1);
        });

        it('should schedule a reconnect', () => (context.policy.schedule as sinon.SinonStub).calledOnce.should.be.true);
    });
}));
