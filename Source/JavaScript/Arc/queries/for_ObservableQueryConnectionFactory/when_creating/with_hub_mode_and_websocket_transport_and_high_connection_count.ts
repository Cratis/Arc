// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_descriptor } from '../given/a_descriptor';
import { given } from '../../../given';
import { Globals } from '../../../Globals';
import { QueryTransportMethod } from '../../QueryTransportMethod';
import { createObservableQueryConnection } from '../../ObservableQueryConnectionFactory';
import { MultiplexedObservableQueryConnection, resetSharedMultiplexer } from '../../ObservableQueryMultiplexer';
import { IObservableQueryConnection } from '../../IObservableQueryConnection';

import * as sinon from 'sinon';

describe('when creating with hub mode and WebSocket transport and high connection count', given(a_descriptor, context => {
    let connection: IObservableQueryConnection<unknown>;
    let warnStub: sinon.SinonStub;
    let originalConnectionCount: number;
    let originalWebSocket: typeof WebSocket;

    beforeEach(() => {
        originalConnectionCount = Globals.queryConnectionCount;
        Globals.queryDirectMode = false;
        Globals.queryTransportMethod = QueryTransportMethod.WebSocket;
        Globals.queryConnectionCount = 10;

        warnStub = sinon.stub(console, 'warn');

        originalWebSocket = global.WebSocket;
        (global as Record<string, unknown>).WebSocket = function () {
            return {
                onopen: null,
                onclose: null,
                onerror: null,
                onmessage: null,
                close: sinon.stub(),
                send: sinon.stub(),
                readyState: 0,
            };
        };

        connection = createObservableQueryConnection(context.descriptor);
    });

    afterEach(() => {
        Globals.queryDirectMode = context.originalDirectMode;
        Globals.queryTransportMethod = context.originalTransportMethod;
        Globals.queryConnectionCount = originalConnectionCount;
        warnStub.restore();
        global.WebSocket = originalWebSocket;
        resetSharedMultiplexer();
    });

    it('should return a MultiplexedObservableQueryConnection', () => {
        connection.should.be.instanceOf(MultiplexedObservableQueryConnection);
    });

    it('should not log a warning', () => {
        warnStub.called.should.be.false;
    });
}));
