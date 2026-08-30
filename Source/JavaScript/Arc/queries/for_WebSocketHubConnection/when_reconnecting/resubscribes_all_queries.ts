// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_web_socket_hub_connection } from '../given/a_web_socket_hub_connection';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';
import type { ReconnectCallback } from '../../IReconnectPolicy';

describe(
    'when reconnecting after a drop',
    given(a_web_socket_hub_connection, (context) => {
        const queryId = 'q1';
        let callbackStub: sinon.SinonStub;

        beforeEach(() => {
            callbackStub = sinon.stub();
            context.setup();
            context.connection.subscribe(queryId, { queryName: 'MyQuery' }, callbackStub);
            context.simulateOpen();
            context.simulateMessage({
                type: HubMessageType.Connected,
                supportsSubscriptionRevisions: true,
            });

            // Simulate drop and policy-driven reconnect
            context.simulateClose();
            const reconnectCallback = (
                context.policy.schedule as sinon.SinonStub
            ).getCall(0).args[0] as ReconnectCallback;
            context.fakeSocket.send.reset();
            (context.policy.reset as sinon.SinonStub).reset();
            reconnectCallback();

            // The new socket opens in legacy mode, then upgrades when the new handshake arrives.
            context.simulateOpen();
            context.simulateMessage({
                type: HubMessageType.Connected,
                supportsSubscriptionRevisions: true,
            });
        });

        afterEach(() => sinon.restore());

        it('should reset to a legacy subscription on the new socket', () => {
            const msg = getSentMessage(0);
            msg.type.should.equal(HubMessageType.Subscribe);
            msg.queryId.should.equal(queryId);
            (msg.revision === undefined).should.equal(true);
        });

        it('should upgrade the reconnect subscription after the new handshake', () => {
            const msg = getSentMessage(1);
            msg.type.should.equal(HubMessageType.Subscribe);
            msg.queryId.should.equal(queryId);
            (msg.revision ?? 0).should.be.greaterThan(0);
        });

        it('should reset the reconnect policy on successful open', () => {
            (context.policy.reset as sinon.SinonStub).calledOnce.should.be.true;
        });

        function getSentMessage(index: number): {
            type: HubMessageType;
            queryId: string;
            revision?: number;
        } {
            try {
                return JSON.parse(context.fakeSocket.send.getCall(index).args[0]) as {
                    type: HubMessageType;
                    queryId: string;
                    revision?: number;
                };
            } catch (error) {
                throw new Error('Expected a valid hub message', { cause: error });
            }
        }
    }),
);
