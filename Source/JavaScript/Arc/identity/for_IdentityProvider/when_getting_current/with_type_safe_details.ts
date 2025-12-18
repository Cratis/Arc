// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '@cratis/fundamentals';
import { IdentityProvider } from '../../IdentityProvider';
import { an_identity_provider } from '../given/an_identity_provider';
import { given } from '../../../given';

class TestDetails {
    userId: Guid = Guid.empty;
    role: string = '';
}

describe('when getting current with type safe details', given(an_identity_provider, () => {
    let result: { id: string; name: string; details: TestDetails };

    beforeEach(async () => {
        const testGuid = Guid.create();
        
        // Mock document.cookie with base64-encoded identity data
        const identityData = {
            id: 'test-user-id',
            name: 'Test User',
            details: { 
                userId: testGuid.toString(), 
                role: 'admin' 
            }
        };
        const encodedData = btoa(JSON.stringify(identityData));
        (global as { document?: { cookie: string } }).document!.cookie = `.cratis-identity=${encodedData}`;

        const identity = await IdentityProvider.getCurrent(TestDetails);
        result = {
            id: identity.id,
            name: identity.name,
            details: identity.details
        };
    });

    it('should deserialize details with proper types', () => {
        result.details.userId.should.be.instanceOf(Guid);
    });
}));
