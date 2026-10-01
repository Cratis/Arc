// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../../IdentityProvider.js';
import { IIdentity } from '../../IIdentity.js';
import { an_identity_provider } from '../given/an_identity_provider.js';
import { given } from '../../../given.js';

/**
 * An identity the server could not resolve is not kept, so a page that loaded before the user signed in sees
 * them once they have.
 */
describe('when getting current after the server did not resolve an identity', given(an_identity_provider, context => {
    let first: IIdentity;
    let second: IIdentity;

    beforeEach(async () => {
        context.fetchStub.onFirstCall().resolves({ ok: false, status: 401 } as Response);
        context.fetchStub.onSecondCall().resolves({
            ok: true,
            json: async () => ({ id: 'user-123', name: 'Test User', roles: [], details: {} })
        } as Response);

        first = await IdentityProvider.getCurrent();
        second = await IdentityProvider.getCurrent();
    });

    it('should report the first identity as not set', () => first.isSet.should.be.false);
    it('should ask the server again', () => context.fetchStub.calledTwice.should.be.true);
    it('should report the identity the server resolved', () => second.id.should.equal('user-123'));
}));
