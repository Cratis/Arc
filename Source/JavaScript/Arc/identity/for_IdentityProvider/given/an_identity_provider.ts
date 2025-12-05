// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { IdentityProvider } from '../../IdentityProvider';

export class an_identity_provider {
    fetchStub: sinon.SinonStub;
    originalApiBasePath: string;
    originalOrigin: string;

    constructor() {
        this.originalApiBasePath = IdentityProvider.apiBasePath;
        this.originalOrigin = IdentityProvider.origin;
        
        // Mock document for tests that need it
        if (typeof document === 'undefined') {
            (global as { document?: { cookie: string } }).document = { cookie: '' };
        }
        
        // Don't create fetch stub in constructor - let each test create it if needed
        this.fetchStub = sinon.stub();
    }
}
