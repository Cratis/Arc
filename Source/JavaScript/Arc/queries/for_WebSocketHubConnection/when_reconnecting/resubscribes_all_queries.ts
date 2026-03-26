// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_web_socket_hub_connection } from '../given/a_web_socket_hub_connection';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';
import { ReconnectCallback } from '../../IReconnectPolicy';

describe('when reconnecting after a drop', given(a_web_socket_hub_connection, context => {
    const queryId = 'q1';
    let callbackStub: sinon.SinonStub;

    beforeEach(() => {
        callbackStub = sinon.stub();
        context.setup();
        context.connection.subscribe(queryId, { queryName: 'MyQuery' }, callbackStub);
        context.simulateOpen();

        // Simulate drop and policy-driven reconnect
        context.simulateClose();
        const reconnectCallback = (context.policy.schedule as sinon.SinonStub).getCall(0).args[0] as ReconnectCallback;
        context.fakeSocket.send.reset();
        (context.policy.reset as sinon.SinonStub).reset();
        reconnectCallback();

        // The new socket opens
        context.simulateOpen();
    });

    afterEach(() => sinon.restore());

    it('should re-send all active subscriptions', () => {
        context.fakeSocket.send.calledOnce.should.be.true;
        const msg = JSON.parse(context.fakeSocket.send.getCall(0).args[0]);
        msg.type.should.equal(HubMessageType.Subscribe);
        msg.queryId.should.equal(queryId);
    });

    it('should reset the reconnect policy on successful open', () => {
        (context.policy.reset as sinon.SinonStub).calledOnce.should.be.true;
    });
}));
