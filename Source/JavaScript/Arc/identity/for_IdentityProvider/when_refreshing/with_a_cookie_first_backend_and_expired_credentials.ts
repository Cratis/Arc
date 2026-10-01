// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../../IdentityProvider.js';
import { IIdentity } from '../../IIdentity.js';
import { a_cookie_first_backend } from '../given/a_cookie_first_backend.js';
import { given } from '../../../given.js';

describe('when refreshing with a cookie-first backend and expired credentials', given(a_cookie_first_backend, context => {
    let refreshed: IIdentity;

    beforeEach(async () => {
        await IdentityProvider.getCurrent();
        context.currentIdentity = undefined;
        refreshed = await IdentityProvider.refresh();
    });

    it('should ask the backend again', () => context.fetchStub.calledTwice.should.be.true);
    it('should report that the identity is no longer set', () => refreshed.isSet.should.be.false);
    it('should not keep the previous principal', () => refreshed.id.should.equal(''));
    it('should not keep the previous roles', () => refreshed.roles.should.be.empty);
}));
