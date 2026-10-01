// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { JSDOM } from 'jsdom';
import { an_identity_provider } from './an_identity_provider.js';
import { IdentityProvider } from '../../IdentityProvider.js';

export class a_browser_with_a_legacy_identity extends an_identity_provider {
    private browser!: JSDOM;
    private originalDocument!: Document;

    constructor() {
        super();
        beforeEach(() => {
            this.originalDocument = document;
            this.browser = new JSDOM('', { url: 'http://localhost/nested/page' });
            global.document = this.browser.window.document;
            document.cookie = `.cratis-identity=${btoa(JSON.stringify({ id: 'user-123', name: 'Original User', roles: ['Reader'], details: {} }))};path=/`;
            this.fetchStub.resolves({ ok: false, status: 404 } as Response);
        });
        afterEach(() => {
            IdentityProvider.clearCache();
            this.browser.window.close();
            global.document = this.originalDocument;
        });
    }
}
