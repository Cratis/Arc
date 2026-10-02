// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../../IdentityProvider.js';
import { IIdentity } from '../../IIdentity.js';
import { a_browser_with_a_legacy_identity } from '../given/a_browser_with_a_legacy_identity.js';
import { given } from '../../../given.js';

describe('when refreshing after the server rejects a retained transition identity', given(a_browser_with_a_legacy_identity, context => {
    let rejected: IIdentity;
    let refreshed: IIdentity;

    beforeEach(async () => {
        await IdentityProvider.refresh();
        context.fetchStub.resolves({ ok: false, status: 401 } as Response);
        rejected = await IdentityProvider.refresh();
        context.fetchStub.resolves({ ok: false, status: 404 } as Response);
        refreshed = await IdentityProvider.refresh();
    });

    it('should not overrule the rejection with the retained identity', () => rejected.isSet.should.be.false);
    it('should discard the transition identity after the rejection', () => refreshed.isSet.should.be.false);
}));
