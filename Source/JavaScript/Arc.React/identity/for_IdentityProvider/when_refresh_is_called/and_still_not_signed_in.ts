// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { given } from '../../../given';
import { an_identity_provider } from '../given/an_identity_provider';
import { BrowserNavigation } from '../../BrowserNavigation';

/**
 * Refreshing an identity that was never signed in and still is not must not reload the page - there is
 * no ended session to recover from, and reloading here would loop a genuinely anonymous caller straight
 * back into the same unresolved state.
 */
describe('when refresh is called and still not signed in', given(an_identity_provider, context => {
    let reloadStub: sinon.SinonStub;

    beforeEach(async () => {
        reloadStub = sinon.stub(BrowserNavigation, 'reload');

        context.fetchStub = context.fetchHelper.stubFetch();
        context.answerNextFetchAsUnauthorized();
        context.renderProvider();
        await context.waitForAsyncUpdates();

        context.answerNextFetchAsUnauthorized();
        await context.refreshIdentity();
    });

    afterEach(() => {
        reloadStub.restore();
        context.cleanup();
    });

    it('should not reload the page', () => reloadStub.called.should.be.false);
}));
