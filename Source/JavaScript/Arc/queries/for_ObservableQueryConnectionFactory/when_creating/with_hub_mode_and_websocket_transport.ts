// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_descriptor } from '../given/a_descriptor';
import { given } from '../../../given';
import { Globals } from '../../../Globals';
import { QueryTransportMethod } from '../../QueryTransportMethod';
import { createObservableQueryConnection } from '../../ObservableQueryConnectionFactory';
import { MultiplexedObservableQueryConnection, WS_HUB_ROUTE } from '../../ObservableQueryMultiplexer';
import { IObservableQueryConnection } from '../../IObservableQueryConnection';

import * as sinon from 'sinon';

describe('when creating with hub mode and WebSocket transport', given(a_descriptor, context => {
    let connection: IObservableQueryConnection<unknown>;
    let webSocketStub: sinon.SinonStub;
    let capturedUrl: string;

    beforeEach(() => {
        Globals.queryDirectMode = false;
        Globals.queryTransportMethod = QueryTransportMethod.WebSocket;

        webSocketStub = sinon.stub(global, 'WebSocket').callsFake((url: string) => {
            capturedUrl = url;
            return {
                onopen: null,
                onclose: null,
                onerror: null,
                onmessage: null,
                close: sinon.stub(),
                send: sinon.stub(),
                readyState: 0,
            } as unknown as WebSocket;
        });

        connection = createObservableQueryConnection(context.descriptor);
    });

    afterEach(() => {
        Globals.queryDirectMode = context.originalDirectMode;
        Globals.queryTransportMethod = context.originalTransportMethod;
        webSocketStub.restore();
    });

    it('should return a MultiplexedObservableQueryConnection', () => {
        connection.should.be.instanceOf(MultiplexedObservableQueryConnection);
    });

    it('should target the WebSocket demultiplexer endpoint when connecting', () => {
        (connection as MultiplexedObservableQueryConnection<unknown>).connect(sinon.stub());
        capturedUrl.should.include(WS_HUB_ROUTE);
    });

    it('should not target the per-query route', () => {
        (connection as MultiplexedObservableQueryConnection<unknown>).connect(sinon.stub());
        capturedUrl.should.not.include('/api/test/item-42');
    });
}));
