// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_descriptor } from '../given/a_descriptor';
import { given } from '../../../given';
import { Globals } from '../../../Globals';
import { QueryTransportMethod } from '../../QueryTransportMethod';
import { createObservableQueryConnection } from '../../ObservableQueryConnectionFactory';
import { SSE_HUB_ROUTE } from '../../ServerSentEventQueryConnection';
import { MultiplexedObservableQueryConnection } from '../../ObservableQueryMultiplexer';
import { IObservableQueryConnection } from '../../IObservableQueryConnection';

import * as sinon from 'sinon';

describe('when creating with hub mode and SSE transport', given(a_descriptor, context => {
    let connection: IObservableQueryConnection<unknown>;
    let capturedUrl: string;

    beforeEach(() => {
        Globals.queryDirectMode = false;
        Globals.queryTransportMethod = QueryTransportMethod.ServerSentEvents;

        const FakeEventSourceConstructor = function (this: EventSource, url: string) {
            capturedUrl = url;
            Object.assign(this, {
                onopen: null,
                onerror: null,
                onmessage: null,
                close: sinon.stub(),
                addEventListener: sinon.stub(),
                removeEventListener: sinon.stub(),
                readyState: 0,
            });
        };
        (globalThis as Record<string, unknown>)['EventSource'] = FakeEventSourceConstructor;

        // Also stub fetch for the POST subscribe/unsubscribe calls
        (globalThis as Record<string, unknown>)['fetch'] = sinon.stub().resolves({ ok: true } as Response);

        connection = createObservableQueryConnection(context.descriptor);
    });

    afterEach(() => {
        Globals.queryDirectMode = context.originalDirectMode;
        Globals.queryTransportMethod = context.originalTransportMethod;
        delete (globalThis as Record<string, unknown>)['EventSource'];
        delete (globalThis as Record<string, unknown>)['fetch'];
    });

    it('should return a MultiplexedObservableQueryConnection', () => {
        connection.should.be.instanceOf(MultiplexedObservableQueryConnection);
    });

    it('should target the SSE demultiplexer endpoint when connecting', () => {
        (connection as MultiplexedObservableQueryConnection<unknown>).connect(sinon.stub());
        capturedUrl.should.include(SSE_HUB_ROUTE);
    });

    it('should not include query name in URL', () => {
        (connection as MultiplexedObservableQueryConnection<unknown>).connect(sinon.stub());
        capturedUrl.should.not.include('query=');
    });

    it('should not include the per-query route', () => {
        (connection as MultiplexedObservableQueryConnection<unknown>).connect(sinon.stub());
        capturedUrl.should.not.include('/api/test/item-42');
    });
}));
