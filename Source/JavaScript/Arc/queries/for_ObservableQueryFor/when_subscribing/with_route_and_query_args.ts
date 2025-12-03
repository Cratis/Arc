// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { an_observable_query_for } from '../given/an_observable_query_for';
import { given } from '../../../given';

import * as sinon from 'sinon';
import { ObservableQuerySubscription } from '../../ObservableQuerySubscription';

describe('when subscribing with route and query args', given(an_observable_query_for, context => {
    let callback: sinon.SinonStub;
    let subscription: ObservableQuerySubscription<string>;
    let webSocketStub: sinon.SinonStub;
    let capturedUrl: string;

    beforeEach(() => {
        context.queryWithRouteAndQueryArgs.setOrigin('https://example.com');
        callback = sinon.stub();

        // Stub WebSocket to capture the URL
        webSocketStub = sinon.stub(global, 'WebSocket').callsFake((url: string) => {
            capturedUrl = url;
            return {
                onopen: null,
                onclose: null,
                onerror: null,
                onmessage: null,
                close: sinon.stub(),
                send: sinon.stub()
            } as unknown as WebSocket;
        });

        // Subscribe with args that include both route parameter (id) and query parameters (filter, limit)
        subscription = context.queryWithRouteAndQueryArgs.subscribe(callback, {
            id: 'my-item-id',
            filter: 'active',
            limit: 50
        });
    });

    afterEach(() => {
        if (subscription) {
            subscription.unsubscribe();
        }
        webSocketStub.restore();
    });

    it('should include route parameter in the path', () => {
        capturedUrl.should.include('/api/items/my-item-id');
    });

    it('should include filter query parameter in the URL', () => {
        capturedUrl.should.include('filter=active');
    });

    it('should include limit query parameter in the URL', () => {
        capturedUrl.should.include('limit=50');
    });
}));
