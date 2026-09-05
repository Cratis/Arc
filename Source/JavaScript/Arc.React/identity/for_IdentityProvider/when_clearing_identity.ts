// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../given';
import { an_identity_provider } from './given/an_identity_provider';

/**
 * Clearing is what logging out does. It answers the question rather than reopening it - there is no
 * request in flight afterwards, so anything gating on the identity has to be told to stop waiting.
 */
describe('when clearing identity', given(an_identity_provider, context => {
    let isSet: boolean;
    let isLoading: boolean;
    let identityId: string;

    beforeEach(async () => {
        context.setupSuccessfulIdentityFetch('user-123', 'John Doe', { role: 'admin' });

        context.renderProvider();
        await context.waitForAsyncUpdates();

        await context.clearIdentity();

        isSet = context.capturedIdentity!.isSet;
        isLoading = context.capturedIdentity!.isLoading;
        identityId = context.capturedIdentity!.id;
    });

    afterEach(() => context.cleanup());

    it('should mark identity as not set', () => isSet.should.be.false);
    it('should forget the identity that was signed in', () => identityId.should.equal(''));
    it('should not report that the identity is loading', () => isLoading.should.be.false);
}));
