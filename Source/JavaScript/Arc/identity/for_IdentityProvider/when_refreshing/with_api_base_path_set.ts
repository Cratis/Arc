// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../../IdentityProvider';
import { an_identity_provider } from '../given/an_identity_provider';
import { given } from '../../../given';

describe('when refreshing with api base path set', given(an_identity_provider, context => {
    let actualUrl: URL;

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
        
        actualUrl = context.fetchStub.firstCall.args[0];
    });

    afterEach(() => {
        IdentityProvider.setApiBasePath('');
    });

    it('should_call_fetch_with_api_base_path_prefixed', () => {
        actualUrl.pathname.should.equal('/custom/api/.cratis/me');
    });
}));
