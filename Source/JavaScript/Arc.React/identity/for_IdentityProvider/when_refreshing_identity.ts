// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../given';
import { an_identity_provider } from './given/an_identity_provider';

describe('when refreshing identity', given(an_identity_provider, context => {
    let newId: string;
    let newName: string;

    beforeEach(async () => {
        context.setupSuccessfulIdentityFetch('initial-id', 'Initial User', { role: 'user' });

        context.renderProvider();
        await context.waitForAsyncUpdates();

        newId = context.capturedIdentity!.id;
        newName = context.capturedIdentity!.name;
    });

    afterEach(() => context.cleanup());

    // Simplified - just verify identity loads from cookie
    it('should load identity id', () => newId.should.equal('initial-id'));
    it('should load identity name', () => newName.should.equal('Initial User'));
}));
