// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { an_observable_query_for } from '../given/an_observable_query_for';
import { given } from '../../../given';
import { Globals } from '../../../Globals';
import { QueryTransportMethod } from '../../QueryTransportMethod';
import { ObservableQuerySubscription } from '../../ObservableQuerySubscription';

import * as sinon from 'sinon';

describe('when subscribing with direct mode and SSE transport', given(an_observable_query_for, context => {
    let callback: sinon.SinonStub;
    let subscription: ObservableQuerySubscription<string>;
    let capturedUrl: string;
    let originalQueryDirectMode: boolean;
    let originalTransportMethod: QueryTransportMethod;

    beforeEach(() => {
        originalQueryDirectMode = Globals.queryDirectMode;
        originalTransportMethod = Globals.queryTransportMethod;

        Globals.queryDirectMode = true;
        Globals.queryTransportMethod = QueryTransportMethod.ServerSentEvents;

        context.query.setOrigin('https://example.com');
        callback = sinon.stub();

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

        subscription = context.query.subscribe(callback, { id: 'test-id' });
    });

    afterEach(() => {
        Globals.queryDirectMode = originalQueryDirectMode;
        Globals.queryTransportMethod = originalTransportMethod;
        if (subscription) {
            subscription.unsubscribe();
        }
        delete (globalThis as Record<string, unknown>)['EventSource'];
    });

    it('should connect to the per-query SSE URL', () => {
        capturedUrl.should.include('/api/test/test-id');
    });

    it('should not connect to the hub SSE endpoint', () => {
        capturedUrl.should.not.include('/.cratis/queries/sse');
    });

    it('should not include a query name parameter', () => {
        capturedUrl.should.not.include('query=');
    });

    it('should return a subscription', () => {
        subscription.should.not.be.undefined;
    });
}));
