// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { an_observable_query_for } from '../given/an_observable_query_for';
import { given } from '../../../given';

import * as sinon from 'sinon';
import { ObservableQuerySubscription } from '../../ObservableQuerySubscription';

describe('when subscribing with partially missing required arguments', given(an_observable_query_for, context => {
    let callback: sinon.SinonStub;
    let subscription: ObservableQuerySubscription<string>;
    let webSocketStub: sinon.SinonStub;

    beforeEach(() => {
        context.queryWithMultipleRequiredParameters.setOrigin('https://example.com');
        callback = sinon.stub();
        webSocketStub = sinon.stub(global, 'WebSocket').callsFake(() => ({
            onopen: null,
            onclose: null,
            onerror: null,
            onmessage: null,
            close: sinon.stub(),
            send: sinon.stub()
        } as unknown as WebSocket));

        subscription = context.queryWithMultipleRequiredParameters.subscribe(callback, {
            userId: 'user-1',
            category: ''
        });
    });

    afterEach(() => {
        if (subscription) {
            subscription.unsubscribe();
        }
        webSocketStub.restore();
    });

    it('should return a subscription', () => {
        subscription.should.not.be.undefined;
    });

    it('should not create a web socket connection', () => {
        webSocketStub.should.not.have.been.called;
    });

    it('should call callback immediately with default value', () => {
        callback.called.should.be.true;
        callback.firstCall.args[0].isSuccess.should.be.true;
    });
}));