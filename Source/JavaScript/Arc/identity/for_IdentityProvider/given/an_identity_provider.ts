// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { IdentityProvider } from '../../IdentityProvider.js';
import { createFetchHelper } from '../../../helpers/fetchHelper.js';

export class an_identity_provider {
    fetchStub: sinon.SinonStub;
    fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    originalApiBasePath: string;
    originalOrigin: string;

    constructor() {
        this.originalApiBasePath = IdentityProvider.apiBasePath;
        this.originalOrigin = IdentityProvider.origin;
        
        // Mock document for tests that need it
        if (typeof document === 'undefined') {
            (global as { document?: { cookie: string; location: { origin: string } } }).document = {
                cookie: '',
                location: {
                    origin: 'http://localhost'
                }
            };
        }
        
        this.fetchHelper = createFetchHelper();
        this.fetchStub = this.fetchHelper.stubFetch();

        // The context is created once per suite, but every spec has to start from a page that has not asked
        // for the identity yet.
        beforeEach(() => {
            (global as { document?: { cookie: string } }).document!.cookie = '';
            IdentityProvider.clearCache();
            this.fetchStub.resetHistory();
        });
    }

    /**
     * Answers every request to `/.cratis/me` with the given identity.
     * @param identity The identity the server reports.
     */
    serverReports(identity: object) {
        this.fetchStub.resolves({
            ok: true,
            json: async () => identity
        } as Response);
    }
}
