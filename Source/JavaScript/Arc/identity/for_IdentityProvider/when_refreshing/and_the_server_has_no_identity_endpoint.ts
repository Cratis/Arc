// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../../IdentityProvider.js';
import { IIdentity } from '../../IIdentity.js';
import { an_identity_provider } from '../given/an_identity_provider.js';
import { given } from '../../../given.js';

describe('when refreshing and the server has no identity endpoint', given(an_identity_provider, context => {
    let refreshed: IIdentity;

    beforeEach(async () => {
        document.cookie = `.cratis-identity=${btoa(JSON.stringify({ id: 'user-123', name: 'Test User', roles: ['Reader'], details: {} }))}`;
        context.fetchStub.resolves({ ok: false, status: 404 } as Response);
        refreshed = await IdentityProvider.refresh();
    });

    it('should ask the server first', () => context.fetchStub.calledOnce.should.be.true);
    it('should preserve the transition fallback', () => refreshed.id.should.equal('user-123'));
    it('should preserve the roles from the fallback', () => refreshed.isInRole('Reader').should.be.true);
}));
