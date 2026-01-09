// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../given';
import { an_identity_provider } from './given/an_identity_provider';

describe('when initial fetch fails', given(an_identity_provider, context => {
    let identityId: string;
    let identityName: string;
    let isSet: boolean;
    let hasRefreshMethod: boolean;

    beforeEach(async () => {
        context.setupFailedIdentityFetch();
        context.suppressConsoleErrors(); // Suppress expected error logs

        context.renderProvider();
        await context.waitForAsyncUpdates();

        identityId = context.capturedIdentity!.id;
        identityName = context.capturedIdentity!.name;
        isSet = context.capturedIdentity!.isSet;
        hasRefreshMethod = typeof context.capturedIdentity!.refresh === 'function';
    });

    afterEach(() => {
        context.restoreConsole();
        context.cleanup();
    });

    it('should have empty identity id', () => identityId.should.equal(''));
    it('should have empty identity name', () => identityName.should.equal(''));
    it('should mark identity as not set', () => isSet.should.be.false);
    it('should still have refresh method available', () => hasRefreshMethod.should.be.true);
}));
