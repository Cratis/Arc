// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../../IdentityProvider.js';
import { IIdentity } from '../../IIdentity.js';
import { a_browser_with_a_legacy_identity } from '../given/a_browser_with_a_legacy_identity.js';
import { given } from '../../../given.js';

describe('when refreshing and the server has no identity endpoint', given(a_browser_with_a_legacy_identity, context => {
    let refreshed: IIdentity;

    beforeEach(async () => {
        refreshed = await IdentityProvider.refresh();
    });

    it('should ask the server first', () => context.fetchStub.calledOnce.should.be.true);
    it('should preserve the transition fallback', () => refreshed.id.should.equal('user-123'));
    it('should preserve the roles from the fallback', () => refreshed.isInRole('Reader').should.be.true);
}));
