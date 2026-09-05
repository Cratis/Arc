// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { an_observable_query_with_validator } from '../given/an_observable_query_with_validator';
import { given } from '../../../given';

import * as sinon from 'sinon';
import { ObservableQuerySubscription } from '../../ObservableQuerySubscription';

describe('when subscribing with client validation passing', given(an_observable_query_with_validator, context => {
    let callback: sinon.SinonStub;
    let subscription: ObservableQuerySubscription<string>;

    beforeEach(() => {
        context.query.setOrigin('https://example.com'); // Set origin to avoid document access
        callback = sinon.stub();

        subscription = context.query.subscribe(callback, { minAge: 18 });
    });

    afterEach(() => {
        if (subscription) {
            subscription.unsubscribe();
        }
    });

    // A real connection is established rather than one that immediately serves a result, which is what
    // distinguishes a subscription that passed validation from one that was rejected by it.
    it('should not call callback immediately', () => {
        callback.called.should.be.false;
    });
}));
