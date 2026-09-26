// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { an_observable_query_for } from '../given/an_observable_query_for.js';
import { given } from '../../../given.js';

import * as sinon from 'sinon';
import { ObservableQuerySubscription } from '../../ObservableQuerySubscription.js';

describe('with enumerable query', given(an_observable_query_for, context => {
    let callback: sinon.SinonStub;
    let subscription: ObservableQuerySubscription<string[]>;

    beforeEach(() => {
        context.enumerableQuery.setOrigin('https://example.com'); // Set origin to avoid document access
        callback = sinon.stub();
        
        subscription = context.enumerableQuery.subscribe(callback, { category: 'test-category' });
    });

    afterEach(() => {
        if (subscription) {
            subscription.unsubscribe();
        }
    });

    it('should return a subscription', () => {
        subscription.should.not.be.undefined;
    });

    it('should not call callback immediately', () => {
        callback.called.should.be.false;
    });
}));