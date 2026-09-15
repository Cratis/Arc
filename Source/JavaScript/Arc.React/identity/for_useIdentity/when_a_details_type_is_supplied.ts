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

describe('when a details type is supplied', () => {
    let useContextStub: sinon.SinonStub;
    let details: UserDetails;

    beforeEach(() => {
        // The provider recorded no detailsConstructor - it never deserialized this payload, so the
        // hook itself has to.
        useContextStub = sinon.stub(React, 'useContext').returns({
            identity: { id: 'user', name: 'user', isSet: true, details: { userId: 'u-1' }, refresh: () => Promise.resolve() },
            detailsConstructor: undefined,
            isLoading: false,
            clearIdentity: () => { },
        });

        details = useIdentity(UserDetails).details;
    });

    afterEach(() => useContextStub.restore());

    it('should deserialize into the supplied type', () => details.should.be.instanceOf(UserDetails));
    it('should deserialize nested concepts too', () => details.userId.should.be.instanceOf(UserId));
    it('should keep the value the server sent', () => details.userId.value.should.equal('u-1'));
});
