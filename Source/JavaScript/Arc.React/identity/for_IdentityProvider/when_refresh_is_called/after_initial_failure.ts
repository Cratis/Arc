// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given';
import { an_identity_provider } from '../given/an_identity_provider';

/**
 * The chicken-and-egg case: the initial load failed, so the identity in state was never built from a
 * server answer. Refresh still has to be reachable on it and still has to work - otherwise a single
 * failed request at startup leaves the application with no way back to a signed-in identity.
 */
describe('when refresh is called after initial failure', given(an_identity_provider, context => {
    let identityId: string;
    let isSet: boolean;

    beforeEach(async () => {
        context.setupFailedIdentityFetch();
        context.suppressConsoleErrors(); // Suppress expected error logs

        context.renderProvider();
        await context.waitForAsyncUpdates();

        context.answerFetchWith('recovered-id', 'Recovered User');
        await context.refreshIdentity();

        identityId = context.capturedIdentity!.id;
        isSet = context.capturedIdentity!.isSet;
    });

    afterEach(() => {
        context.restoreConsole();
        context.cleanup();
    });

    it('should recover the identity', () => identityId.should.equal('recovered-id'));
    it('should mark identity as set', () => isSet.should.be.true);
}));
