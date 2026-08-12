// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given';
import { an_identity_provider } from '../given/an_identity_provider';

describe('when refresh is called and succeeds', given(an_identity_provider, context => {
    let identityId: string;
    let isLoading: boolean;

    beforeEach(async () => {
        context.setupSuccessfulIdentityFetch('initial-id', 'Initial User', { role: 'user' });

        context.renderProvider();
        await context.waitForAsyncUpdates();

        context.answerFetchWith('refreshed-id', 'Refreshed User', { role: 'admin' });
        await context.refreshIdentity();

        identityId = context.capturedIdentity!.id;
        isLoading = context.capturedIdentity!.isLoading;
    });

    afterEach(() => context.cleanup());

    it('should replace the identity with the refreshed one', () => identityId.should.equal('refreshed-id'));
    it('should stop reporting that the identity is loading', () => isLoading.should.be.false);
}));
