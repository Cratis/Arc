// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given';
import { an_identity_provider } from '../given/an_identity_provider';

/**
 * A refresh clears the identity cookie on its way out, so a failed one leaves the identity in hand
 * older than the credential it came from. That is the caller's problem to act on - but only if the
 * provider stops claiming a request is still in flight.
 */
describe('when refresh is called and fails', given(an_identity_provider, context => {
    let isLoading: boolean;
    let rejection: unknown;

    beforeEach(async () => {
        context.setupSuccessfulIdentityFetch('initial-id', 'Initial User', {});

        context.renderProvider();
        await context.waitForAsyncUpdates();

        context.failEveryFetch();
        rejection = await context.refreshIdentity();

        isLoading = context.capturedIdentity!.isLoading;
    });

    afterEach(() => context.cleanup());

    it('should stop reporting that the identity is loading', () => isLoading.should.be.false);
    it('should let the caller know the refresh failed', () => rejection!.should.be.instanceOf(Error));
}));
