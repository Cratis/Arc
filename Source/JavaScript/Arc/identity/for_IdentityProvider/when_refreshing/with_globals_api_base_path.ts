// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../../IdentityProvider';
import { Globals } from '../../../Globals';
import { an_identity_provider } from '../given/an_identity_provider';
import { given } from '../../../given';

describe('when refreshing with globals api base path', given(an_identity_provider, context => {
    let originalGlobalsApiBasePath: string;

    beforeEach(async () => {
        context.fetchStub.resolves({
            ok: true,
            json: async () => ({
                id: 'test-user-id',
                name: 'Test User',
                details: { role: 'admin' }
            })
        } as Response);

        originalGlobalsApiBasePath = Globals.apiBasePath;
        Globals.apiBasePath = '/global/api';
        IdentityProvider.setApiBasePath('');
        await IdentityProvider.refresh();
    });

    afterEach(() => {
        Globals.apiBasePath = originalGlobalsApiBasePath;
        IdentityProvider.setApiBasePath('');
    });

    it('should call fetch with globals api base path prefixed', () => {
        context.fetchStub.should.have.been.calledWith('/global/api/.cratis/me');
    });
}));
