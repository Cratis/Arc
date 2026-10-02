// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../../IdentityProvider.js';
import { IIdentity } from '../../IIdentity.js';
import { a_browser_with_a_legacy_identity } from '../given/a_browser_with_a_legacy_identity.js';
import { given } from '../../../given.js';

// The proxy may authorize from its own cache without reissuing its readable cookie.
describe('when refreshing consecutively without an identity endpoint', given(a_browser_with_a_legacy_identity, context => {
    let refreshed: IIdentity;

    beforeEach(async () => {
        await IdentityProvider.refresh();
        refreshed = await IdentityProvider.refresh();
    });

    it('should ask the server for each refresh', () => context.fetchStub.calledTwice.should.be.true);
    it('should retain the transition identity', () => refreshed.id.should.equal('user-123'));
    it('should retain the transition roles', () => refreshed.isInRole('Reader').should.be.true);
    it('should restore the root cookie for navigation', () => document.cookie.should.contain('.cratis-identity='));
    it('should retain SameSite Lax on HTTP', () => context.cookieJar.getCookiesSync('http://localhost/')[0].sameSite!.should.equal('lax'));
    it('should not require HTTPS on an HTTP site', () => context.cookieJar.getCookiesSync('http://localhost/')[0].secure.should.be.false);
}));
