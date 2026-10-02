// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../../IdentityProvider.js';
import { IIdentity } from '../../IIdentity.js';
import { an_identity_provider } from '../given/an_identity_provider.js';
import { given } from '../../../given.js';

describe('when getting current and the server rejects the caller with a forged identity cookie', given(an_identity_provider, context => {
    let identity: IIdentity;

    beforeEach(async () => {
        const forged = btoa(JSON.stringify({ id: 'admin', name: 'Forged Administrator', roles: ['admin'], details: {} }));
        (global as { document?: { cookie: string } }).document!.cookie = `.cratis-identity=${forged}`;
        context.fetchStub.resolves({ ok: false, status: 401 } as Response);

        identity = await IdentityProvider.getCurrent();
    });

    it('should not be set', () => identity.isSet.should.be.false);
    it('should not report the forged identity', () => identity.id.should.equal(''));
    it('should not report the forged role', () => identity.isInRole('admin').should.be.false);
}));
