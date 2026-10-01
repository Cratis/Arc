// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../../IdentityProvider.js';
import { IIdentity } from '../../IIdentity.js';
import { an_identity_provider } from '../given/an_identity_provider.js';
import { given } from '../../../given.js';

describe('when getting current twice', given(an_identity_provider, context => {
    let first: IIdentity;
    let second: IIdentity;

    beforeEach(async () => {
        context.serverReports({ id: 'user-123', name: 'Test User', roles: [], details: {} });

        first = await IdentityProvider.getCurrent();
        second = await IdentityProvider.getCurrent();
    });

    it('should ask the server only once', () => context.fetchStub.calledOnce.should.be.true);
    it('should report the same identity both times', () => second.id.should.equal(first.id));
}));
