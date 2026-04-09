// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_web_socket_hub_connection } from '../given/a_web_socket_hub_connection';
import { given } from '../../../given';

describe('when unsubscribing the only query, nulls socket handlers before closing the socket', given(a_web_socket_hub_connection, context => {
    let handlersWereNullWhenClosed: { onopen: boolean; onclose: boolean; onerror: boolean; onmessage: boolean };

    beforeEach(() => {
        context.setup();
        context.connection.subscribe('q1', { queryName: 'MyQuery' }, sinon.stub());
        context.simulateOpen();

        handlersWereNullWhenClosed = { onopen: false, onclose: false, onerror: false, onmessage: false };
        context.fakeSocket.close.callsFake(() => {
            handlersWereNullWhenClosed.onopen = context.fakeSocket.onopen === null;
            handlersWereNullWhenClosed.onclose = context.fakeSocket.onclose === null;
            handlersWereNullWhenClosed.onerror = context.fakeSocket.onerror === null;
            handlersWereNullWhenClosed.onmessage = context.fakeSocket.onmessage === null;
        });

        context.connection.unsubscribe('q1');
    });

    afterEach(() => sinon.restore());

    it('should have nulled onopen before calling socket.close()', () => handlersWereNullWhenClosed.onopen.should.be.true);
    it('should have nulled onclose before calling socket.close()', () => handlersWereNullWhenClosed.onclose.should.be.true);
    it('should have nulled onerror before calling socket.close()', () => handlersWereNullWhenClosed.onerror.should.be.true);
    it('should have nulled onmessage before calling socket.close()', () => handlersWereNullWhenClosed.onmessage.should.be.true);
}));
