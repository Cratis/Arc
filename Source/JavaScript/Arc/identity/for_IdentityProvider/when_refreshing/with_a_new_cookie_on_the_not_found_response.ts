// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../../IdentityProvider.js';
import { IIdentity } from '../../IIdentity.js';
import { a_browser_with_a_legacy_identity } from '../given/a_browser_with_a_legacy_identity.js';
import { given } from '../../../given.js';

describe('when refreshing with a new cookie on the not found response', given(a_browser_with_a_legacy_identity, context => {
    let refreshed: IIdentity;

    beforeEach(async () => {
        context.fetchStub.callsFake(async () => {
            document.cookie = `.cratis-identity=${btoa(JSON.stringify({ id: 'user-456', name: 'Updated User', roles: ['Writer'], details: {} }))};path=/`;
            return { ok: false, status: 404 } as Response;
        });
        refreshed = await IdentityProvider.refresh();
    });

    it('should prefer the identity issued by the response', () => refreshed.id.should.equal('user-456'));
    it('should use the updated roles', () => refreshed.isInRole('Writer').should.be.true);
    it('should not retain the earlier roles', () => refreshed.isInRole('Reader').should.be.false);
}));
