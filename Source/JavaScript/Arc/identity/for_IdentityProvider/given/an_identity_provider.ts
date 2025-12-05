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
        
        // Restore any existing fetch stub before creating a new one
        if ((globalThis.fetch as sinon.SinonStub)?.restore) {
            (globalThis.fetch as sinon.SinonStub).restore();
        }
        this.fetchStub = sinon.stub(globalThis, 'fetch');
    }
}
