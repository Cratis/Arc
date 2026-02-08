// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../../IdentityProvider';
import { Globals } from '../../../Globals';
import { an_identity_provider } from '../given/an_identity_provider';
import { given } from '../../../given';

describe('when refreshing without api base path', given(an_identity_provider, context => {
    let originalGlobalsApiBasePath: string;
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

        originalGlobalsApiBasePath = Globals.apiBasePath;
        Globals.apiBasePath = '';
        IdentityProvider.setApiBasePath('');
        await IdentityProvider.refresh();
        
        actualUrl = context.fetchStub.firstCall.args[0];
    });

    afterEach(() => {
        Globals.apiBasePath = originalGlobalsApiBasePath;
    });

    it('should call fetch with default path', () => {
        actualUrl.pathname.should.equal('/.cratis/me');
    });
}));
