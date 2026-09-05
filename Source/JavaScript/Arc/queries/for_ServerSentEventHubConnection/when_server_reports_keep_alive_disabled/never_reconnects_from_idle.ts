// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_server_sent_event_hub_connection } from '../given/a_server_sent_event_hub_connection';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';

const ASSUMED_KEEP_ALIVE_MS = 500;

describe('when the server reports that keep-alive is disabled', given(a_server_sent_event_hub_connection, context => {
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
            keepAliveIntervalMs: 0
        });

        // A server that never pings is silent by design — watching for silence would otherwise
        // reconnect on a loop forever. Hard drops still surface through onerror.
        clock.tick(ASSUMED_KEEP_ALIVE_MS * 20);
    });

    afterEach(() => {
        clock.restore();
        sinon.restore();
    });

    it('should never schedule a reconnect from inactivity', () => (context.policy.schedule as sinon.SinonStub).called.should.be.false);
}));
