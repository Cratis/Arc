// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../given';
import { an_identity_provider } from './given/an_identity_provider';

describe('when initial fetch succeeds', given(an_identity_provider, context => {
    let identityId: string;
    let identityName: string;
    let isSet: boolean;

    beforeEach(async () => {
        context.setupSuccessfulIdentityFetch('user-123', 'John Doe', { role: 'admin' });

        context.renderProvider();
        await context.waitForAsyncUpdates();

        identityId = context.capturedIdentity!.id;
        identityName = context.capturedIdentity!.name;
        isSet = context.capturedIdentity!.isSet;
    });

    afterEach(() => context.cleanup());

    it('should set identity id', () => identityId.should.equal('user-123'));
    it('should set identity name', () => identityName.should.equal('John Doe'));
    it('should mark identity as set', () => isSet.should.be.true);
    it('should have refresh method', () => context.capturedIdentity!.refresh.should.be.a('function'));
}));
