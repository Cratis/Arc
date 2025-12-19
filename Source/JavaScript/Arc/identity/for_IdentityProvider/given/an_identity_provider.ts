// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { IdentityProvider } from '../../IdentityProvider';
import { createFetchHelper } from '../../../helpers/fetchHelper';

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
            (global as { document?: { cookie: string } }).document = { cookie: '' };
        }
        
        this.fetchHelper = createFetchHelper();
        this.fetchStub = this.fetchHelper.stubFetch();
    }
}
