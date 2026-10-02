// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { field, Guid } from '@cratis/fundamentals';
import { IdentityProvider } from '../../IdentityProvider.js';
import { an_identity_provider } from '../given/an_identity_provider.js';
import { given } from '../../../given.js';

class TestDetails {
    @field(Guid)
    userId!: Guid;

    @field(String)
    role!: string;
}

describe('when getting current with type safe details', given(an_identity_provider, context => {
    let result: { id: string; name: string; details: TestDetails };
    let testGuid: Guid;

    beforeEach(async () => {
        testGuid = Guid.create();

        const identityData = {
            id: 'test-user-id',
            name: 'Test User',
            details: {
                userId: testGuid.toString(),
                role: 'admin'
            }
        };
        context.serverReports(identityData);

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

    it('should carry over the value the server sent', () => {
        result.details.userId.toString().should.equal(testGuid.toString());
        result.details.role.should.equal('admin');
    });
}));
