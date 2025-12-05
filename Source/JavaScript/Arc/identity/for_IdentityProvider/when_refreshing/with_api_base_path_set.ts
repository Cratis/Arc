// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../../IdentityProvider';
import { an_identity_provider } from '../given/an_identity_provider';
import { given } from '../../../given';
import sinon from 'sinon';

// eslint-disable-next-line @typescript-eslint/no-unused-vars
describe('when refreshing with api base path set', given(an_identity_provider, context => {
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

        IdentityProvider.setApiBasePath('/custom/api');
        await IdentityProvider.refresh();
    });

    afterEach(() => {
        IdentityProvider.setApiBasePath('');
        fetchStub.restore();
    });

    it('should call fetch with api base path prefixed', () => {
        fetchStub.should.have.been.calledWith('/custom/api/.cratis/me');
    });
}));
