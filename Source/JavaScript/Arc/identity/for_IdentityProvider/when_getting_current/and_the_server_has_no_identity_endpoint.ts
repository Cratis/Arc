// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../../IdentityProvider.js';
import { IIdentity } from '../../IIdentity.js';
import { an_identity_provider } from '../given/an_identity_provider.js';
import { given } from '../../../given.js';

/**
 * An application whose identity comes from a proxy in front of it may have no `/.cratis/me` of its own, and earlier
 * versions read the identity from the cookie that proxy writes. Until the proxy serves the endpoint, the cookie is
 * still read in that one case - loudly, so the dependency does not go unnoticed.
 */
describe('when getting current and the server has no identity endpoint', given(an_identity_provider, context => {
    let identity: IIdentity;
    let warnings: unknown[][];
    let originalConsoleWarn: typeof console.warn;

    beforeEach(async () => {
        warnings = [];
        originalConsoleWarn = console.warn;
        console.warn = (...args: unknown[]) => { warnings.push(args); };
        (IdentityProvider as unknown as { hasWarnedAboutLegacyCookie: boolean }).hasWarnedAboutLegacyCookie = false;

        const cookie = btoa(JSON.stringify({ id: 'user-123', name: 'Test User', roles: ['Reader'], details: {} }));
        (global as { document?: { cookie: string } }).document!.cookie = `.cratis-identity=${cookie}`;
        context.fetchStub.resolves({ ok: false, status: 404 } as Response);

        identity = await IdentityProvider.getCurrent();
    });

    afterEach(() => { console.warn = originalConsoleWarn; });

    it('should ask the server first', () => context.fetchStub.calledOnce.should.be.true);
    it('should report the identity from the cookie', () => identity.id.should.equal('user-123'));
    it('should report the roles from the cookie', () => identity.isInRole('Reader').should.be.true);
    it('should warn that the cookie was read', () => warnings.length.should.equal(1));
}));
