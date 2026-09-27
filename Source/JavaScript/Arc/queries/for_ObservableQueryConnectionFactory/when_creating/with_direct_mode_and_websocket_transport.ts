// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_descriptor } from '../given/a_descriptor.js';
import { given } from '../../../given.js';
import { Globals } from '../../../Globals.js';
import { QueryTransportMethod } from '../../QueryTransportMethod.js';
import { createObservableQueryConnection } from '../../ObservableQueryConnectionFactory.js';
import { ObservableQueryConnection } from '../../ObservableQueryConnection.js';
import { IObservableQueryConnection } from '../../IObservableQueryConnection.js';

import * as sinon from 'sinon';

describe('when creating with direct mode and WebSocket transport', given(a_descriptor, context => {
    let connection: IObservableQueryConnection<unknown>;
    let originalWebSocket: typeof WebSocket;
    let capturedUrl: string;

    beforeEach(() => {
        Globals.queryDirectMode = true;
        Globals.queryTransportMethod = QueryTransportMethod.WebSocket;

        originalWebSocket = global.WebSocket;
        (global as Record<string, unknown>).WebSocket = function (url: string) {
            capturedUrl = url;
            return {
                onopen: null,
                onclose: null,
                onerror: null,
                onmessage: null,
                close: sinon.stub(),
                send: sinon.stub(),
            };
        };

        connection = createObservableQueryConnection(context.descriptor);
    });

    afterEach(() => {
        Globals.queryDirectMode = context.originalDirectMode;
        Globals.queryTransportMethod = context.originalTransportMethod;
        global.WebSocket = originalWebSocket;
        sinon.restore();
    });

    it('should return an ObservableQueryConnection', () => {
        connection.should.be.instanceOf(ObservableQueryConnection);
    });

    it('should target the per-query URL with route parameters replaced', () => {
        (connection as ObservableQueryConnection<unknown>).connect(sinon.stub());
        capturedUrl.should.include('/api/test/item-42');
    });

    it('should not target the hub endpoint', () => {
        (connection as ObservableQueryConnection<unknown>).connect(sinon.stub());
        capturedUrl.should.not.include('/.cratis/queries');
    });
}));
