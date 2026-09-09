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

describe('when the provider already deserialized', () => {
    let useContextStub: sinon.SinonStub;
    let providerDeserializedDetails: UserDetails;
    let details: UserDetails;

    beforeEach(() => {
        providerDeserializedDetails = new UserDetails();
        providerDeserializedDetails.userId = new UserId('u-1');

        // detailsConstructor carries the type the provider already deserialized this exact payload
        // with - the signal that there is nothing left for the hook to do.
        useContextStub = sinon.stub(React, 'useContext').returns({
            identity: { id: 'user', name: 'user', isSet: true, details: providerDeserializedDetails, refresh: () => Promise.resolve() },
            detailsConstructor: UserDetails,
            isLoading: false,
            clearIdentity: () => { },
        });

        details = useIdentity(UserDetails).details;
    });

    afterEach(() => useContextStub.restore());

    it('should hand back the very same instance', () => details.should.equal(providerDeserializedDetails));
    it('should not wrap the concept a second time', () => details.userId.value.should.equal('u-1'));
});
