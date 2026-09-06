// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import sinon from 'sinon';
import { ConceptAs, field } from '@cratis/fundamentals';
import { useIdentity } from '../useIdentity';

class UserId extends ConceptAs<string> { }

class UserDetails {
    @field(UserId)
    userId!: UserId;
}

describe('when the same identity is read twice', () => {
    let useContextStub: sinon.SinonStub;
    let first: UserDetails;
    let second: UserDetails;

    beforeEach(() => {
        // The provider never deserialized this payload (no detailsConstructor), so both reads have
        // to go through the hook's own deserialization - and should land on the same cached instance.
        useContextStub = sinon.stub(React, 'useContext').returns({
            identity: { id: 'user', name: 'user', isSet: true, details: { userId: 'u-1' }, refresh: () => Promise.resolve() },
            detailsConstructor: undefined,
            isLoading: false,
            clearIdentity: () => { },
        });

        first = useIdentity(UserDetails).details;
        second = useIdentity(UserDetails).details;
    });

    afterEach(() => useContextStub.restore());

    it('should hand back the same instance both times', () => first.should.equal(second));
});
