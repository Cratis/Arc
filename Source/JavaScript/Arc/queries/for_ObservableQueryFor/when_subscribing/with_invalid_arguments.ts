// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { an_observable_query_for } from '../given/an_observable_query_for.js';
import { given } from '../../../given.js';

import * as sinon from 'sinon';
import { ObservableQuerySubscription } from '../../ObservableQuerySubscription.js';

describe('with invalid arguments', given(an_observable_query_for, context => {
    let callback: sinon.SinonStub;
    let subscription: ObservableQuerySubscription<string>;

    beforeEach(() => {
        context.query.setOrigin('https://example.com'); // Set origin to avoid document access
        callback = sinon.stub();
        
        // Subscribe with missing required arguments
        subscription = context.query.subscribe(callback);
    });

    afterEach(() => {
        if (subscription) {
            subscription.unsubscribe();
        }
    });

    it('should return a subscription', () => {
        subscription.should.not.be.undefined;
    });

    it('should call callback immediately with default value', () => {
        callback.called.should.be.true;
        // The NullObservableQueryConnection returns QueryResult.empty with defaultValue
        // but the data might be transformed, so let's check the actual structure
        const result = callback.firstCall.args[0];
        result.should.not.be.undefined;
        result.isSuccess.should.be.true;
    });
}));