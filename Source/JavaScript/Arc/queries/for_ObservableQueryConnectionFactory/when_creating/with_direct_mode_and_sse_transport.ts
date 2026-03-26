// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_descriptor } from '../given/a_descriptor';
import { given } from '../../../given';
import { Globals } from '../../../Globals';
import { QueryTransportMethod } from '../../QueryTransportMethod';
import { createObservableQueryConnection } from '../../ObservableQueryConnectionFactory';
import { ServerSentEventQueryConnection } from '../../ServerSentEventQueryConnection';
import { IObservableQueryConnection } from '../../IObservableQueryConnection';

import * as sinon from 'sinon';

describe('when creating with direct mode and SSE transport', given(a_descriptor, context => {
    let connection: IObservableQueryConnection<unknown>;
    let capturedUrl: string;

    beforeEach(() => {
        Globals.queryDirectMode = true;
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
            });
        };
        (globalThis as Record<string, unknown>)['EventSource'] = FakeEventSourceConstructor;

        connection = createObservableQueryConnection(context.descriptor);
    });

    afterEach(() => {
        Globals.queryDirectMode = context.originalDirectMode;
        Globals.queryTransportMethod = context.originalTransportMethod;
        delete (globalThis as Record<string, unknown>)['EventSource'];
    });

    it('should return a ServerSentEventQueryConnection', () => {
        connection.should.be.instanceOf(ServerSentEventQueryConnection);
    });

    it('should target the per-query URL with route parameters replaced', () => {
        (connection as ServerSentEventQueryConnection<unknown>).connect(sinon.stub());
        capturedUrl.should.include('/api/test/item-42');
    });

    it('should not target the hub SSE endpoint', () => {
        (connection as ServerSentEventQueryConnection<unknown>).connect(sinon.stub());
        capturedUrl.should.not.include('/.cratis/queries/sse');
    });

    it('should not include a query name parameter', () => {
        (connection as ServerSentEventQueryConnection<unknown>).connect(sinon.stub());
        capturedUrl.should.not.include('query=');
    });
}));
