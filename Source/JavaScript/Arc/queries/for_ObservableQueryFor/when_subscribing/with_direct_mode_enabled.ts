// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { an_observable_query_for } from '../given/an_observable_query_for';
import { given } from '../../../given';
import { Globals } from '../../../Globals';
import { ObservableQuerySubscription } from '../../ObservableQuerySubscription';

import * as sinon from 'sinon';

describe('when subscribing with direct mode enabled', given(an_observable_query_for, context => {
    let callback: sinon.SinonStub;
    let subscription: ObservableQuerySubscription<string>;
    let webSocketStub: sinon.SinonStub;
    let capturedUrl: string;
    let originalQueryDirectMode: boolean;

    beforeEach(() => {
        originalQueryDirectMode = Globals.queryDirectMode;
        Globals.queryDirectMode = true;

        context.query.setOrigin('https://example.com');
        callback = sinon.stub();

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

        subscription = context.query.subscribe(callback, { id: 'test-id' });
    });

    afterEach(() => {
        Globals.queryDirectMode = originalQueryDirectMode;
        if (subscription) {
            subscription.unsubscribe();
        }
        webSocketStub.restore();
    });

    it('should connect directly to the per-query WebSocket URL', () => {
        capturedUrl.should.include('/api/test/test-id');
    });

    it('should return a subscription', () => {
        subscription.should.not.be.undefined;
    });
}));
