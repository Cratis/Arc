// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../../IdentityProvider';
import { an_identity_provider } from '../given/an_identity_provider';
import { given } from '../../../given';

// Deliberately undecorated - no @field members. Deserializing into this would construct
// `new UndecoratedDetails()` and copy nothing, discarding whatever the server sent.
class UndecoratedDetails {
    userId!: string;
    role!: string;
}

describe('when getting current with a details type that declares no fields', given(an_identity_provider, () => {
    let originalConsoleWarn: typeof console.warn;
    let result: { details: UndecoratedDetails };

    beforeEach(async () => {
        originalConsoleWarn = console.warn;
        console.warn = () => { /* Suppressed during test */ };

        const identityData = {
            id: 'test-user-id',
            name: 'Test User',
            details: {
                userId: 'u-1',
                role: 'admin'
            }
        };
        const encodedData = btoa(JSON.stringify(identityData));
        (global as { document?: { cookie: string } }).document!.cookie = `.cratis-identity=${encodedData}`;

        const identity = await IdentityProvider.getCurrent(UndecoratedDetails);
        result = { details: identity.details };
    });

    afterEach(() => { console.warn = originalConsoleWarn; });

    it('should hand back the payload untouched', () => {
        (result.details as unknown as { userId: string }).userId.should.equal('u-1');
        (result.details as unknown as { role: string }).role.should.equal('admin');
    });

    it('should not blank the details', () => {
        result.details.should.not.be.instanceOf(UndecoratedDetails);
    });
}));
