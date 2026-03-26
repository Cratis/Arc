// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_web_socket_hub_connection } from '../given/a_web_socket_hub_connection';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';

const PING_INTERVAL_MS = 500;

describe('when a message is received and then keep-alive interval elapses', given(a_web_socket_hub_connection, context => {
    let clock: sinon.SinonFakeTimers;

    beforeEach(() => {
        clock = sinon.useFakeTimers();
        context.setup();

        const { WebSocketHubConnection } = require('../../WebSocketHubConnection');
        context.connection = new WebSocketHubConnection(
            'ws://localhost/.cratis/queries/ws',
            '',
            PING_INTERVAL_MS,
            context.policy
        );

        context.connection.subscribe('q1', { queryName: 'MyQuery' }, sinon.stub());
        context.simulateOpen();

        // Advance to just before the ping interval.
        clock.tick(PING_INTERVAL_MS - 50);

        // A message arrives — this resets the inactivity timer.
        context.simulateMessage({ type: HubMessageType.QueryResult, queryId: 'q1', payload: { items: [], totalItems: 0 } });

        // Advance another half interval — total elapsed is PING_INTERVAL_MS + PING_INTERVAL_MS/2 - 50,
        // but only PING_INTERVAL_MS/2 has passed since the last activity.
        clock.tick(PING_INTERVAL_MS / 2);
    });

    afterEach(() => {
        clock.restore();
        sinon.restore();
    });

    it('should not send a ping', () => {
        const sentMessages: string[] = context.fakeSocket.send.args.map((a: string[]) => a[0]);
        const pingMessages = sentMessages
            .map((m: string) => JSON.parse(m))
            .filter((m: { type: HubMessageType }) => m.type === HubMessageType.Ping);
        pingMessages.length.should.equal(0);
    });
}));
