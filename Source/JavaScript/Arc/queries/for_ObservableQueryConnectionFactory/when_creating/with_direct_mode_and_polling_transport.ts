// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { given } from '../../../given.js';
import { Globals } from '../../../Globals.js';
import { a_descriptor } from '../given/a_descriptor.js';
import { QueryTransportMethod } from '../../QueryTransportMethod.js';
import { PollingQueryConnection } from '../../PollingQueryConnection.js';
import { createObservableQueryConnection } from '../../ObservableQueryConnectionFactory.js';
import { getSharedMultiplexer, resetSharedMultiplexer } from '../../ObservableQueryMultiplexer.js';
import { IObservableQueryConnection } from '../../IObservableQueryConnection.js';

describe('when creating with direct mode and polling transport', given(a_descriptor, context => {
    let connection: IObservableQueryConnection<unknown>;
    let eventSource: sinon.SinonStub;
    let webSocket: sinon.SinonStub;
    const originalEventSource = Object.getOwnPropertyDescriptor(globalThis, 'EventSource');
    const originalWebSocket = Object.getOwnPropertyDescriptor(globalThis, 'WebSocket');

    beforeEach(() => {
        resetSharedMultiplexer();
        Globals.queryDirectMode = true;
        Globals.queryTransportMethod = QueryTransportMethod.Polling;
        eventSource = sinon.stub().throws('EventSource must not be used');
        webSocket = sinon.stub().throws('WebSocket must not be used');
        Object.defineProperty(globalThis, 'EventSource', { configurable: true, value: eventSource });
        Object.defineProperty(globalThis, 'WebSocket', { configurable: true, value: webSocket });
        connection = createObservableQueryConnection(context.descriptor);
    });
    afterEach(() => {
        connection?.disconnect();
        Globals.queryDirectMode = context.originalDirectMode;
        Globals.queryTransportMethod = context.originalTransportMethod;
        if (originalEventSource) Object.defineProperty(globalThis, 'EventSource', originalEventSource);
        else delete (globalThis as Record<string, unknown>).EventSource;
        if (originalWebSocket) Object.defineProperty(globalThis, 'WebSocket', originalWebSocket);
        else delete (globalThis as Record<string, unknown>).WebSocket;
        resetSharedMultiplexer();
    });
    it('should return a polling connection', () => connection.should.be.instanceOf(PollingQueryConnection));
    it('should not create persistent transports', () => {
        eventSource.called.should.be.false;
        webSocket.called.should.be.false;
    });
    it('should not create a multiplexer', () => (getSharedMultiplexer() === undefined).should.be.true);
}));
