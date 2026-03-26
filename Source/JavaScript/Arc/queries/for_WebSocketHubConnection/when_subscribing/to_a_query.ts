// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_web_socket_hub_connection } from '../given/a_web_socket_hub_connection';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';

describe('when subscribing to a query', given(a_web_socket_hub_connection, context => {
    const queryId = 'q1';
    const request = { queryName: 'MyQuery', arguments: {} };
    let callback: sinon.SinonStub;

    beforeEach(() => {
        callback = sinon.stub();
        context.setup();
        context.connection.subscribe(queryId, request, callback);
        context.simulateOpen();
    });

    afterEach(() => sinon.restore());

    it('should open a WebSocket', () => context.WebSocketStub.calledOnce.should.be.true);

    it('should send a subscribe message once the socket opens', () => {
        context.fakeSocket.send.calledOnce.should.be.true;
        const msg = JSON.parse(context.fakeSocket.send.getCall(0).args[0]);
        msg.type.should.equal(HubMessageType.Subscribe);
        msg.queryId.should.equal(queryId);
    });

    describe('when a query result message arrives', () => {
        const result = { isSuccess: true, data: [1, 2, 3] };

        beforeEach(() => {
            context.simulateMessage({ type: HubMessageType.QueryResult, queryId, payload: result });
        });

        it('should invoke the callback with the result', () => callback.calledOnce.should.be.true);
        it('should pass the result payload', () => callback.getCall(0).args[0].should.deep.equal(result));
    });
}));
