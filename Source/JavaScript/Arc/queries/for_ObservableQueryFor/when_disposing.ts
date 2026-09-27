// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { an_observable_query_for } from './given/an_observable_query_for.js';
import { given } from '../../given.js';

import * as sinon from 'sinon';

describe('when disposing', given(an_observable_query_for, context => {
    let callback: sinon.SinonStub;

    beforeEach(() => {
        context.query.setOrigin('https://example.com'); // Set origin to avoid document access
        callback = sinon.stub();
        
        // Create a subscription first
        context.query.subscribe(callback, { id: 'test-id' });
    });

    it('should not throw when disposing', () => {
        (() => context.query.dispose()).should.not.throw();
    });

    it('should be safe to dispose multiple times', () => {
        context.query.dispose();
        (() => context.query.dispose()).should.not.throw();
    });
}));