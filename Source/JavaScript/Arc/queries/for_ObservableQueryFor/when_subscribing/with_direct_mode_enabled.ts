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
    let originalWebSocket: typeof WebSocket;
    let capturedUrl: string;
    let originalQueryDirectMode: boolean;

    beforeEach(() => {
        originalQueryDirectMode = Globals.queryDirectMode;
        Globals.queryDirectMode = true;

        context.query.setOrigin('https://example.com');
        callback = sinon.stub();

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

        subscription = context.query.subscribe(callback, { id: 'test-id' });
    });

    afterEach(() => {
        Globals.queryDirectMode = originalQueryDirectMode;
        if (subscription) {
            subscription.unsubscribe();
        }
        global.WebSocket = originalWebSocket;
        sinon.restore();
    });

    it('should connect directly to the per-query WebSocket URL', () => {
        capturedUrl.should.include('/api/test/test-id');
    });

    it('should return a subscription', () => {
        subscription.should.not.be.undefined;
    });
}));
