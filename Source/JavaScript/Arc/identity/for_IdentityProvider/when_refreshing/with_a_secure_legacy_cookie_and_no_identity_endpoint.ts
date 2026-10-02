// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../../IdentityProvider.js';
import { IIdentity } from '../../IIdentity.js';
import { a_secure_browser_with_a_legacy_identity } from '../given/a_secure_browser_with_a_legacy_identity.js';
import { given } from '../../../given.js';

describe('when refreshing a secure legacy cookie without an identity endpoint', given(a_secure_browser_with_a_legacy_identity, context => {
    let refreshed: IIdentity;

    beforeEach(async () => {
        await IdentityProvider.refresh();
        refreshed = await IdentityProvider.refresh();
    });

    it('should retain the transition identity', () => refreshed.id.should.equal('user-123'));
    it('should restore the cookie for HTTPS navigation', () => context.cookieJar.getCookieStringSync('https://identity.example/').should.contain('.cratis-identity='));
    it('should not send the restored cookie over HTTP', () => context.cookieJar.getCookieStringSync('http://identity.example/').should.equal(''));
    it('should retain SameSite Lax', () => context.cookieJar.getCookiesSync('https://identity.example/')[0].sameSite!.should.equal('lax'));
}));
