// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_web_socket_hub_connection } from '../given/a_web_socket_hub_connection';
import { given } from '../../../given';
import { ReconnectCallback } from '../../IReconnectPolicy';

describe('when the connection closes unexpectedly', given(a_web_socket_hub_connection, context => {
    beforeEach(() => {
        context.setup();
        context.connection.subscribe('q1', { queryName: 'MyQuery' }, sinon.stub());
        context.simulateOpen();
        context.simulateClose();
    });

    afterEach(() => sinon.restore());

    it('should delegate to the reconnect policy', () => {
        (context.policy.schedule as sinon.SinonStub).calledOnce.should.be.true;
    });

    describe('when the policy fires the reconnect callback', () => {
        let secondSocketCreated: boolean;

        beforeEach(() => {
            secondSocketCreated = false;
            context.WebSocketStub.callsFake(() => {
                secondSocketCreated = true;
                return context.fakeSocket;
            });

            // Simulate the policy invoking the callback immediately
            const scheduleCall = (context.policy.schedule as sinon.SinonStub).getCall(0);
            const reconnectCallback = scheduleCall.args[0] as ReconnectCallback;
            reconnectCallback();
        });

        it('should open a new WebSocket', () => secondSocketCreated.should.be.true);
    });
}));
