// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_web_socket_hub_connection } from '../given/a_web_socket_hub_connection';
import { given } from '../../../given';

describe('when opening while the owned socket is closing', given(a_web_socket_hub_connection, context => {
    let firstSocket: typeof context.fakeSocket;

    beforeEach(() => {
        context.setup();
        context.connection.subscribe('q1', { queryName: 'First' }, sinon.stub());
        firstSocket = context.fakeSocket;
        firstSocket.readyState = WebSocket.CLOSING;
        context.WebSocketStub.onSecondCall().returns({
            onopen: null,
            onclose: null,
            onerror: null,
            onmessage: null,
            send: sinon.stub(),
            close: sinon.stub(),
            readyState: WebSocket.CONNECTING,
        });
        context.connection.subscribe('q2', { queryName: 'Second' }, sinon.stub());
    });

    afterEach(() => {
        context.connection.dispose();
        sinon.restore();
    });

    it('should close the replaced socket', () => firstSocket.close.calledOnce.should.be.true);
    it('should detach the replaced socket open handler', () => (firstSocket.onopen === null).should.be.true);
    it('should detach the replaced socket close handler', () => (firstSocket.onclose === null).should.be.true);
    it('should detach the replaced socket error handler', () => (firstSocket.onerror === null).should.be.true);
    it('should detach the replaced socket message handler', () => (firstSocket.onmessage === null).should.be.true);
    it('should create one replacement socket', () => context.WebSocketStub.callCount.should.equal(2));
}));
