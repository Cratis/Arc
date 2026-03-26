// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_server_sent_event_hub_connection } from '../given/a_server_sent_event_hub_connection';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';

describe('when disposing the hub connection', given(a_server_sent_event_hub_connection, context => {
    beforeEach(() => {
        context.setup();
        context.connection.subscribe('q1', { queryName: 'MyQuery' }, sinon.stub());
        context.simulateOpen();
        context.simulateMessage({ type: HubMessageType.Connected, payload: 'conn-1' });
        context.connection.dispose();
    });

    afterEach(() => sinon.restore());

    it('should close the EventSource', () => context.fakeEventSource.close.calledOnce.should.be.true);
    it('should cancel the reconnect policy', () => (context.policy.cancel as sinon.SinonStub).calledOnce.should.be.true);
    it('should report zero active subscriptions', () => context.connection.queryCount.should.equal(0));

    describe('when an error event fires after dispose', () => {
        beforeEach(() => {
            (context.policy.schedule as sinon.SinonStub).reset();
            context.simulateError();
        });

        it('should not schedule a reconnect', () => {
            (context.policy.schedule as sinon.SinonStub).callCount.should.equal(0);
        });
    });
}));
