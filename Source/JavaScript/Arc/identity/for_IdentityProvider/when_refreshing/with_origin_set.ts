// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../../IdentityProvider';
import { an_identity_provider } from '../given/an_identity_provider';
import { given } from '../../../given';

describe('when refreshing with origin set', given(an_identity_provider, context => {
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

        IdentityProvider.setOrigin('https://example.com');
        IdentityProvider.setApiBasePath('/api/v1');
        
        await IdentityProvider.refresh();
        
        actualUrl = context.fetchStub.firstCall.args[0];
    });

    afterEach(() => {
        IdentityProvider.setOrigin('');
        IdentityProvider.setApiBasePath('');
    });

    it('should_call_fetch_with_full_url_including_origin', () => {
        actualUrl.href.should.equal('https://example.com/api/v1/.cratis/me');
    });

    it('should_have_correct_origin', () => {
        actualUrl.origin.should.equal('https://example.com');
    });

    it('should_have_correct_pathname', () => {
        actualUrl.pathname.should.equal('/api/v1/.cratis/me');
    });
}));
