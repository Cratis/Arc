// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../../IdentityProvider.js';
import { IIdentity } from '../../IIdentity.js';
import { an_identity_provider } from '../given/an_identity_provider.js';
import { given } from '../../../given.js';

describe('when refreshing after getting current', given(an_identity_provider, context => {
    let refreshed: IIdentity;
    let current: IIdentity;

    beforeEach(async () => {
        context.serverReports({ id: 'user-123', name: 'Test User', roles: [], details: {} });
        await IdentityProvider.getCurrent();

        context.serverReports({ id: 'user-123', name: 'Renamed User', roles: [], details: {} });
        refreshed = await IdentityProvider.refresh();
        current = await IdentityProvider.getCurrent();
    });

    it('should ask the server again', () => context.fetchStub.calledTwice.should.be.true);
    it('should report what the server reports now', () => refreshed.name.should.equal('Renamed User'));
    it('should keep the refreshed identity for the next caller', () => current.name.should.equal('Renamed User'));
}));
