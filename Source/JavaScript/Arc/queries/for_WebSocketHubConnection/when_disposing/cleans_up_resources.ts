// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_web_socket_hub_connection } from '../given/a_web_socket_hub_connection';
import { given } from '../../../given';

describe('when disposing the hub connection', given(a_web_socket_hub_connection, context => {
    beforeEach(() => {
        context.setup();
        context.connection.subscribe('q1', { queryName: 'MyQuery' }, sinon.stub());
        context.simulateOpen();
        context.connection.dispose();
    });

    afterEach(() => sinon.restore());

    it('should close the socket', () => context.fakeSocket.close.calledOnce.should.be.true);
    it('should cancel the reconnect policy', () => (context.policy.cancel as sinon.SinonStub).calledOnce.should.be.true);

    describe('when the socket fires onclose after dispose', () => {
        beforeEach(() => {
            (context.policy.schedule as sinon.SinonStub).reset();
            context.simulateClose();
        });

        it('should not schedule a reconnect', () => {
            (context.policy.schedule as sinon.SinonStub).callCount.should.equal(0);
        });
    });
}));
