// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { an_observable_query_for } from '../given/an_observable_query_for';
import { given } from '../../../given';
import { Globals } from '../../../Globals';
import { QueryTransportMethod } from '../../QueryTransportMethod';
import { ObservableQuerySubscription } from '../../ObservableQuerySubscription';
import { SSE_HUB_ROUTE } from '../../ServerSentEventQueryConnection';
import { resetSharedMultiplexer } from '../../ObservableQueryMultiplexer';

import * as sinon from 'sinon';

describe('when subscribing with direct mode disabled and SSE transport', given(an_observable_query_for, context => {
    let callback: sinon.SinonStub;
    let subscription: ObservableQuerySubscription<string>;
    let capturedUrl: string;
    let originalQueryDirectMode: boolean;
    let originalTransportMethod: QueryTransportMethod;

    beforeEach(() => {
        originalQueryDirectMode = Globals.queryDirectMode;
        originalTransportMethod = Globals.queryTransportMethod;
        resetSharedMultiplexer();

        Globals.queryDirectMode = false;
        Globals.queryTransportMethod = QueryTransportMethod.ServerSentEvents;

        context.query.setOrigin('https://example.com');
        callback = sinon.stub();

        // EventSource doesn't exist in Node.js — inject a fake via globalThis
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
        resetSharedMultiplexer();
    });

    it('should connect to the centralized SSE hub endpoint', () => {
        capturedUrl.should.include(SSE_HUB_ROUTE);
    });

    it('should not include query name in URL since subscriptions are done via POST', () => {
        capturedUrl.should.not.include('query=');
    });

    it('should return a subscription', () => {
        subscription.should.not.be.undefined;
    });
}));
