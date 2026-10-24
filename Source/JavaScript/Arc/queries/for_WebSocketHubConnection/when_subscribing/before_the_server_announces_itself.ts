// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_web_socket_hub_connection } from '../given/a_web_socket_hub_connection.js';
import { given } from '../../../given.js';
import { HubMessageType, WebSocketHubConnection } from '../../WebSocketHubConnection.js';

describe(
    'when the socket opens but the server has not announced itself',
    given(a_web_socket_hub_connection, (context) => {
        let clock: sinon.SinonFakeTimers;

        beforeEach(() => {
            clock = sinon.useFakeTimers();
            context.setup();
            context.connection.subscribe('q1', { queryName: 'MyQuery' }, sinon.stub());
            context.simulateOpenWithoutAnnouncement();
        });

        afterEach(() => {
            clock.restore();
            sinon.restore();
        });

        it('should hold the subscription back', () =>
            context.fakeSocket.send.called.should.equal(false));

        it('should send nothing more than a subscription without a revision once the grace period passes', () => {
            clock.tick(WebSocketHubConnection.ConnectedGraceMs);
            context.fakeSocket.send.calledOnce.should.equal(true);
            const message = JSON.parse(context.fakeSocket.send.firstCall.args[0]);
            message.type.should.equal(HubMessageType.Subscribe);
            (message.revision === undefined).should.equal(true);
        });

        it('should not send it a second time when the announcement arrives late', () => {
            clock.tick(WebSocketHubConnection.ConnectedGraceMs);
            context.simulateMessage({ type: HubMessageType.Connected, supportsSubscriptionRevisions: true });
            context.fakeSocket.send.calledOnce.should.equal(true);
        });
    }),
);
