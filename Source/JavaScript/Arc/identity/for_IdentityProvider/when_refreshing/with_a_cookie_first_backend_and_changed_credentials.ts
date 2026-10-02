// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../../IdentityProvider.js';
import { IIdentity } from '../../IIdentity.js';
import { a_cookie_first_backend } from '../given/a_cookie_first_backend.js';
import { given } from '../../../given.js';

describe('when refreshing with a cookie-first backend and changed credentials', given(a_cookie_first_backend, context => {
    let refreshed: IIdentity;
    let current: IIdentity;

    beforeEach(async () => {
        await IdentityProvider.getCurrent();
        context.currentIdentity = { id: 'user-456', name: 'Current User', roles: ['Editor'], details: { department: 'Current' } };
        refreshed = await IdentityProvider.refresh();
        current = await IdentityProvider.getCurrent();
    });

    it('should ask the backend again', () => context.fetchStub.calledTwice.should.be.true);
    it('should report the current principal', () => refreshed.id.should.equal('user-456'));
    it('should report the current name', () => refreshed.name.should.equal('Current User'));
    it('should report the current roles', () => refreshed.roles.should.deep.equal(['Editor']));
    it('should report the recomputed details', () => refreshed.details.should.deep.equal({ department: 'Current' }));
    it('should cache the refreshed identity', () => current.id.should.equal('user-456'));
    it('should expire the legacy cookie at the root path', () => document.cookie.should.equal('.cratis-identity=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=/'));
}));
