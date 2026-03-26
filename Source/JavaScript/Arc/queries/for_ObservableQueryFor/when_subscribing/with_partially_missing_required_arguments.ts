// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { an_observable_query_for } from '../given/an_observable_query_for';
import { given } from '../../../given';

import * as sinon from 'sinon';
import { ObservableQuerySubscription } from '../../ObservableQuerySubscription';

describe('when subscribing with partially missing required arguments', given(an_observable_query_for, context => {
    let callback: sinon.SinonStub;
    let subscription: ObservableQuerySubscription<string>;
    let originalWebSocket: typeof WebSocket;
    let webSocketCalled: boolean;

    beforeEach(() => {
        context.queryWithMultipleRequiredParameters.setOrigin('https://example.com');
        callback = sinon.stub();
        webSocketCalled = false;

        originalWebSocket = global.WebSocket;
        (global as Record<string, unknown>).WebSocket = function () {
            webSocketCalled = true;
            return {
                onopen: null,
                onclose: null,
                onerror: null,
                onmessage: null,
                close: sinon.stub(),
                send: sinon.stub(),
            };
        };

        subscription = context.queryWithMultipleRequiredParameters.subscribe(callback, {
            userId: 'user-1',
            category: ''
        });
    });

    afterEach(() => {
        if (subscription) {
            subscription.unsubscribe();
        }
        global.WebSocket = originalWebSocket;
        sinon.restore();
    });

    it('should return a subscription', () => {
        subscription.should.not.be.undefined;
    });

    it('should not create a web socket connection', () => {
        webSocketCalled.should.be.false;
    });

    it('should call callback immediately with default value', () => {
        callback.called.should.be.true;
        callback.firstCall.args[0].isSuccess.should.be.true;
    });
}));