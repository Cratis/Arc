// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given';
import { an_identity_provider } from '../given/an_identity_provider';

/**
 * The loading flag is not a first-paint concern. A refresh is how an application picks up a sign-in or
 * a freshly granted role, and until it answers the identity in hand is the old one - so the flag has
 * to go back up, or every consumer reads stale state as settled fact.
 */
describe('when refresh is called and is in flight', given(an_identity_provider, context => {
    let isLoading: boolean;
    let identityId: string;

    beforeEach(async () => {
        context.setupSuccessfulIdentityFetch('initial-id', 'Initial User', { role: 'user' });

        context.renderProvider();
        await context.waitForAsyncUpdates();

        await context.beginRefresh();

        isLoading = context.capturedIdentity!.isLoading;
        identityId = context.capturedIdentity!.id;
    });

    afterEach(() => context.cleanup());

    it('should report that the identity is loading', () => isLoading.should.be.true);
    it('should still carry the identity from before the refresh', () => identityId.should.equal('initial-id'));
}));
