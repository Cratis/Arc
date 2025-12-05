// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../../IdentityProvider';
import { Globals } from '../../../Globals';
import { an_identity_provider } from '../given/an_identity_provider';
import { given } from '../../../given';
import sinon from 'sinon';

// eslint-disable-next-line @typescript-eslint/no-unused-vars
describe('when refreshing without api base path', given(an_identity_provider, context => {
    let originalGlobalsApiBasePath: string;
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    let fetchStub: sinon.SinonStub;

    beforeEach(async () => {
        fetchStub = sinon.stub(globalThis, 'fetch');
        fetchStub.resolves({
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
    });

    afterEach(() => {
        Globals.apiBasePath = originalGlobalsApiBasePath;
        fetchStub.restore();
    });

    it('should call fetch with default path', () => {
        fetchStub.should.have.been.calledWith('/.cratis/me');
    });
}));
