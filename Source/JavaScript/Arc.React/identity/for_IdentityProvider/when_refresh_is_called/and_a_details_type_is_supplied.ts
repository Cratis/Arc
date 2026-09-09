// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { field, Guid } from '@cratis/fundamentals';
import { given } from '../../../given';
import { an_identity_provider } from '../given/an_identity_provider';

class UserDetails {
    @field(Guid)
    userId!: Guid;
}

describe('when refresh is called and a details type is supplied', given(an_identity_provider, context => {
    let refreshedUserId: Guid;
    let details: UserDetails;

    beforeEach(async () => {
        context.setupSuccessfulIdentityFetch('initial-id', 'Initial User', { userId: Guid.create().toString() });

        context.renderProvider(UserDetails);
        await context.waitForAsyncUpdates();

        refreshedUserId = Guid.create();
        context.answerFetchWith('refreshed-id', 'Refreshed User', { userId: refreshedUserId.toString() });
        await context.refreshIdentity();

        details = context.capturedIdentity!.details as UserDetails;
    });

    afterEach(() => context.cleanup());

    it('should deserialize the refreshed details into the supplied type', () => details.should.be.instanceOf(UserDetails));
    it('should keep the value the refreshed response sent', () => details.userId.toString().should.equal(refreshedUserId.toString()));
}));
