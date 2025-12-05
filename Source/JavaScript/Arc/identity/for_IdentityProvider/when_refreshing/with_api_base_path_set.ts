// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../../IdentityProvider';
import { an_identity_provider } from '../given/an_identity_provider';
import { given } from '../../../given';

describe('when refreshing with api base path set', given(an_identity_provider, context => {
    beforeEach(async () => {
        context.fetchStub.resolves({
            ok: true,
            json: async () => ({
                id: 'test-user-id',
                name: 'Test User',
                details: { role: 'admin' }
            })
        } as Response);

        IdentityProvider.setApiBasePath('/custom/api');
        await IdentityProvider.refresh();
    });

    afterEach(() => {
        IdentityProvider.setApiBasePath('');
    });

    it('should call fetch with api base path prefixed', () => {
        context.fetchStub.should.have.been.calledWith('/custom/api/.cratis/me');
    });
}));
