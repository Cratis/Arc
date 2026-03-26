// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_web_socket_hub_connection } from '../given/a_web_socket_hub_connection';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';

describe('when unsubscribing the only query', given(a_web_socket_hub_connection, context => {
    const queryId = 'q1';

    beforeEach(() => {
        context.setup();
        context.connection.subscribe(queryId, { queryName: 'MyQuery' }, sinon.stub());
        context.simulateOpen();
        context.fakeSocket.send.reset();
        context.connection.unsubscribe(queryId);
    });

    afterEach(() => sinon.restore());

    it('should send an unsubscribe message', () => {
        context.fakeSocket.send.calledOnce.should.be.true;
        const msg = JSON.parse(context.fakeSocket.send.getCall(0).args[0]);
        msg.type.should.equal(HubMessageType.Unsubscribe);
        msg.queryId.should.equal(queryId);
    });

    it('should close the underlying socket', () => context.fakeSocket.close.calledOnce.should.be.true);
}));
