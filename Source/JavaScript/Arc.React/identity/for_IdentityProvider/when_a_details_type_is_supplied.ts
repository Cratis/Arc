// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { field, Guid } from '@cratis/fundamentals';
import { given } from '../../given';
import { an_identity_provider } from './given/an_identity_provider';

class UserDetails {
    @field(Guid)
    userId!: Guid;
}

describe('when a details type is supplied', given(an_identity_provider, context => {
    let userId: Guid;
    let details: UserDetails;

    beforeEach(async () => {
        userId = Guid.create();
        context.setupSuccessfulIdentityFetch('user-123', 'John Doe', { userId: userId.toString() });

        context.renderProvider(UserDetails);
        await context.waitForAsyncUpdates();

        details = context.capturedIdentity!.details as UserDetails;
    });

    afterEach(() => context.cleanup());

    it('should deserialize the details into the supplied type', () => details.should.be.instanceOf(UserDetails));
    it('should keep the value the server sent', () => details.userId.toString().should.equal(userId.toString()));
}));
