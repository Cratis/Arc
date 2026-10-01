// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../IdentityProvider.js';
import { IIdentity } from '../IIdentity.js';
import { an_identity_provider } from './given/an_identity_provider.js';
import { given } from '../../given.js';

describe('when clearing cache', given(an_identity_provider, context => {
    let identity: IIdentity;

    beforeEach(async () => {
        context.serverReports({ id: 'user-123', name: 'Test User', roles: [], details: {} });
        await IdentityProvider.getCurrent();

        IdentityProvider.clearCache();
        context.fetchStub.resolves({ ok: false, status: 401 } as Response);
        identity = await IdentityProvider.getCurrent();
    });

    it('should ask the server again', () => context.fetchStub.calledTwice.should.be.true);
    it('should report what the server reports now', () => identity.isSet.should.be.false);
}));
