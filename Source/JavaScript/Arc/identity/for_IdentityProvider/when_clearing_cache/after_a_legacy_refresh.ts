// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../../IdentityProvider.js';
import { IIdentity } from '../../IIdentity.js';
import { a_browser_with_a_legacy_identity } from '../given/a_browser_with_a_legacy_identity.js';
import { given } from '../../../given.js';

describe('when clearing cache after a legacy refresh', given(a_browser_with_a_legacy_identity, context => {
    let identity: IIdentity;

    beforeEach(async () => {
        await IdentityProvider.refresh();
        IdentityProvider.clearCache();
        identity = await IdentityProvider.getCurrent();
    });

    it('should ask the server again', () => context.fetchStub.calledTwice.should.be.true);
    it('should discard the retained transition identity', () => identity.isSet.should.be.false);
    it('should leave the legacy cookie expired', () => document.cookie.should.equal(''));
}));
