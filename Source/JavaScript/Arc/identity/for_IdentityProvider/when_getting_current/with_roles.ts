// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../../IdentityProvider';
import { an_identity_provider } from '../given/an_identity_provider';
import { given } from '../../../given';

describe('when getting current with roles', given(an_identity_provider, () => {
    let identity: { id: string; name: string; roles: string[]; isInRole: (role: string) => boolean };

    beforeEach(async () => {
        const identityData = {
            id: 'test-user-id',
            name: 'Test User',
            roles: ['Admin', 'User'],
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

    it('should have the roles', () => {
        identity.roles.should.deep.equal(['Admin', 'User']);
    });

    it('should return true when checking for Admin role', () => {
        identity.isInRole('Admin').should.be.true;
    });

    it('should return true when checking for User role', () => {
        identity.isInRole('User').should.be.true;
    });

    it('should return false when checking for non-existent role', () => {
        identity.isInRole('SuperAdmin').should.be.false;
    });
}));
