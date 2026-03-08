// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../../IdentityProvider';
import { an_identity_provider } from '../given/an_identity_provider';
import { given } from '../../../given';

describe('when getting current without roles', given(an_identity_provider, () => {
    let identity: { id: string; name: string; roles: string[]; isInRole: (role: string) => boolean };

    beforeEach(async () => {
        const identityData = {
            id: 'test-user-id',
            name: 'Test User',
            details: {}
        };
        const encodedData = btoa(JSON.stringify(identityData));
        (global as { document?: { cookie: string } }).document!.cookie = `.cratis-identity=${encodedData}`;

        const result = await IdentityProvider.getCurrent();
        identity = {
            id: result.id,
            name: result.name,
            roles: result.roles,
            isInRole: result.isInRole
        };
    });

    it('should have empty roles array', () => {
        identity.roles.should.be.empty;
    });

    it('should return false when checking for any role', () => {
        identity.isInRole('Admin').should.be.false;
    });
}));
