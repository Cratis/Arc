// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../../IdentityProvider.js';
import { IIdentity } from '../../IIdentity.js';
import { an_identity_provider } from '../given/an_identity_provider.js';
import { given } from '../../../given.js';

/**
 * A script on the page, or on a sibling subdomain, can write a `.cratis-identity` cookie. Earlier versions read
 * the identity from it, so whoever wrote it decided who the page thought the user was.
 */
describe('when getting current with a forged identity cookie', given(an_identity_provider, context => {
    let identity: IIdentity;

    beforeEach(async () => {
        const forged = btoa(JSON.stringify({ id: 'admin', name: 'Forged Administrator', roles: ['admin'], details: {} }));
        (global as { document?: { cookie: string } }).document!.cookie = `.cratis-identity=${forged}`;
        context.serverReports({ id: 'user-123', name: 'Test User', roles: ['Reader'], details: {} });

        identity = await IdentityProvider.getCurrent();
    });

    it('should ask the server', () => context.fetchStub.calledOnce.should.be.true);
    it('should ask the identity endpoint', () => String(context.fetchStub.firstCall.args[0]).should.contain('/.cratis/me'));
    it('should report the identity the server reported', () => identity.id.should.equal('user-123'));
    it('should report the roles the server reported', () => identity.roles.should.deep.equal(['Reader']));
    it('should not report the forged role', () => identity.isInRole('admin').should.be.false);
}));
