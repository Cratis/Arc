// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { an_observable_query_for } from '../given/an_observable_query_for';
import { given } from '../../../given';
import { Globals } from '../../../Globals';
import { QueryTransportMethod } from '../../QueryTransportMethod';
import { ObservableQuerySubscription } from '../../ObservableQuerySubscription';
import { SSE_HUB_ROUTE } from '../../ServerSentEventQueryConnection';

import * as sinon from 'sinon';

describe('when subscribing with direct mode disabled and SSE transport', given(an_observable_query_for, context => {
    let callback: sinon.SinonStub;
    let subscription: ObservableQuerySubscription<string>;
    let eventSourceStub: sinon.SinonStub;
    let capturedUrl: string;
    let originalQueryDirectMode: boolean;
    let originalTransportMethod: QueryTransportMethod;

    beforeEach(() => {
        originalQueryDirectMode = Globals.queryDirectMode;
        originalTransportMethod = Globals.queryTransportMethod;

        Globals.queryDirectMode = false;
        Globals.queryTransportMethod = QueryTransportMethod.ServerSentEvents;

        context.query.setOrigin('https://example.com');
        callback = sinon.stub();

        eventSourceStub = sinon.stub(global, 'EventSource').callsFake((url: string) => {
            capturedUrl = url;
            return {
                onopen: null,
                onerror: null,
                onmessage: null,
                close: sinon.stub(),
                addEventListener: sinon.stub(),
                removeEventListener: sinon.stub()
            } as unknown as EventSource;
        });

        subscription = context.query.subscribe(callback, { id: 'test-id' });
    });

    afterEach(() => {
        Globals.queryDirectMode = originalQueryDirectMode;
        Globals.queryTransportMethod = originalTransportMethod;
        if (subscription) {
            subscription.unsubscribe();
        }
        eventSourceStub.restore();
    });

    it('should connect to the centralized SSE hub endpoint', () => {
        capturedUrl.should.include(SSE_HUB_ROUTE);
    });

    it('should include the query name parameter', () => {
        capturedUrl.should.include('query=');
    });

    it('should return a subscription', () => {
        subscription.should.not.be.undefined;
    });
}));
