// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { JSDOM } from 'jsdom';
import { an_identity_provider } from './an_identity_provider.js';
import { IdentityProvider } from '../../IdentityProvider.js';

export class a_browser_with_a_legacy_identity extends an_identity_provider {
    private browser!: JSDOM;
    private originalDocument!: Document;
    private originalLocation: Location | undefined;
    protected browserUrl = 'http://localhost/nested/page';

    get cookieJar() {
        return this.browser.cookieJar;
    }

    constructor() {
        super();
        beforeEach(() => {
            this.originalDocument = document;
            this.originalLocation = global.location;
            this.browser = new JSDOM('', { url: this.browserUrl });
            global.document = this.browser.window.document;
            global.location = this.browser.window.location;
            document.cookie = `.cratis-identity=${btoa(JSON.stringify({ id: 'user-123', name: 'Original User', roles: ['Reader'], details: {} }))};path=/;samesite=lax${location.protocol === 'https:' ? ';secure' : ''}`;
            this.fetchStub.resolves({ ok: false, status: 404 } as Response);
        });
        afterEach(() => {
            IdentityProvider.clearCache();
            this.browser.window.close();
            global.document = this.originalDocument;
            if (this.originalLocation) global.location = this.originalLocation;
            else delete (global as { location?: Location }).location;
        });
    }

    reload(): void {
        const cookieJar = this.browser.cookieJar;
        const url = this.browser.window.location.href;
        this.browser.window.close();
        this.browser = new JSDOM('', { url, cookieJar });
        global.document = this.browser.window.document;
        global.location = this.browser.window.location;
    }
}
