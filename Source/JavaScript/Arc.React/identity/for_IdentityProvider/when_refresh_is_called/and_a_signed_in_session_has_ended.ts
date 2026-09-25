// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { given } from '../../../given';
import { an_identity_provider } from '../given/an_identity_provider';
import { BrowserNavigation } from '../../BrowserNavigation';

/**
 * A refresh that resolves an identity that was signed in into one that is not means the session ended
 * server-side while this tab held on to it - the ticket expired, or access was revoked. Settling into
 * the unset identity here would leave the tab stuck: every command would keep failing silently, and
 * whatever observable connections were opened while still authenticated would keep streaming stale
 * data. A reload sends the browser through an ordinary top-level navigation instead, which AuthProxy
 * already answers correctly by redirecting to sign in - see Cratis/AuthProxy#136.
 */
describe('when refresh is called and a signed-in session has ended', given(an_identity_provider, context => {
    let reloadStub: sinon.SinonStub;

    beforeEach(async () => {
        reloadStub = sinon.stub(BrowserNavigation, 'reload');

        context.setupSuccessfulIdentityFetch('initial-id', 'Initial User', {});

        context.renderProvider();
        await context.waitForAsyncUpdates();

        context.answerNextFetchAsUnauthorized();
        await context.refreshIdentity();
    });

    afterEach(() => {
        reloadStub.restore();
        context.cleanup();
    });

    it('should reload the page', () => reloadStub.calledOnce.should.be.true);
}));
