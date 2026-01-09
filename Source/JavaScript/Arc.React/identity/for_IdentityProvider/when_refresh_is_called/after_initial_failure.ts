// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given';
import { an_identity_provider } from '../given/an_identity_provider';

describe('when refresh is called after initial failure', given(an_identity_provider, context => {
    beforeEach(async () => {
        context.setupFailedIdentityFetch();
        
        // Suppress console errors during this test
        context.suppressConsoleErrors();

        context.renderProvider();
        await context.waitForAsyncUpdates();
    });

    afterEach(() => {
        context.restoreConsole();
        context.cleanup();
    });

    // Test proves the chicken-and-egg fix: refresh() is available even after initial failure
    // This is the key fix - before, if getCurrent() failed, there was no way to get a working identity
    it('should have empty identity id', () => context.capturedIdentity!.id.should.equal(''));
    it('should have empty identity name', () => context.capturedIdentity!.name.should.equal(''));
    it('should mark identity as not set', () => context.capturedIdentity!.isSet.should.be.false);
    it('should have refresh method available for retry', () => (typeof context.capturedIdentity!.refresh).should.equal('function'));
}));
