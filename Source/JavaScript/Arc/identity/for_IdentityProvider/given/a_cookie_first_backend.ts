// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { an_identity_provider } from './an_identity_provider.js';
import { IdentityProviderResult } from '../../IdentityProviderResult.js';

export class a_cookie_first_backend extends an_identity_provider {
    currentIdentity: IdentityProviderResult | undefined;

    constructor() {
        super();
        beforeEach(() => {
            this.currentIdentity = { id: 'user-123', name: 'Original User', roles: ['Reader'], details: { department: 'Original' } };
            document.cookie = `.cratis-identity=${btoa(JSON.stringify(this.currentIdentity))}`;
            this.fetchStub.callsFake(async () => {
                // Earlier backends return the readable cookie before checking credentials or recomputing details.
                const encodedCookie = document.cookie.split(';').map(_ => _.trim()).find(_ => _.startsWith('.cratis-identity='))?.substring('.cratis-identity='.length);
                const identity = encodedCookie ? JSON.parse(atob(encodedCookie)) : this.currentIdentity;
                return identity
                    ? { ok: true, status: 200, json: async () => identity } as Response
                    : { ok: false, status: 401 } as Response;
            });
        });
    }
}
