// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_web_socket_hub_connection } from '../given/a_web_socket_hub_connection';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';

const PING_INTERVAL_MS = 500;

describe('when keep-alive interval elapses without any message', given(a_web_socket_hub_connection, context => {
    let clock: sinon.SinonFakeTimers;

    beforeEach(() => {
        clock = sinon.useFakeTimers();
        context.setup();

        // Construct a new connection with a short ping interval so tests are fast.
        const { WebSocketHubConnection } = require('../../WebSocketHubConnection');
        context.connection = new WebSocketHubConnection(
            'ws://localhost/.cratis/queries/ws',
            '',
            PING_INTERVAL_MS,
            context.policy
        );

        context.connection.subscribe('q1', { queryName: 'MyQuery' }, sinon.stub());
        context.simulateOpen();

        // Advance past the ping interval without sending any messages.
        clock.tick(PING_INTERVAL_MS + 1);
    });

    afterEach(() => {
        clock.restore();
        sinon.restore();
    });

    it('should send a ping message', () => {
        const sentMessages: string[] = context.fakeSocket.send.args.map((a: string[]) => a[0]);
        const pingMessages = sentMessages
            .map((m: string) => JSON.parse(m))
            .filter((m: { type: HubMessageType }) => m.type === HubMessageType.Ping);
        pingMessages.length.should.be.greaterThan(0);
    });
}));
