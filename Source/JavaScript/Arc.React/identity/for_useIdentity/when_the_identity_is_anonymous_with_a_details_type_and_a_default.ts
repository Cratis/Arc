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

describe('when the identity is anonymous with a details type and a default', () => {
    let useContextStub: sinon.SinonStub;
    let defaultDetails: UserDetails;
    let details: UserDetails;

    beforeEach(() => {
        defaultDetails = new UserDetails();
        defaultDetails.userId = new UserId('default');

        // isSet: false is the anonymous/not-yet-resolved sentinel - its `details` is `{}`. A details
        // type is also supplied here, which must not cause `{}` to be run through deserialization
        // instead of falling back to the default.
        useContextStub = sinon.stub(React, 'useContext').returns({
            identity: { id: '', name: '', isSet: false, details: {}, refresh: () => Promise.resolve() },
            detailsConstructor: undefined,
            isLoading: false,
            clearIdentity: () => { },
        });

        details = useIdentity(UserDetails, defaultDetails).details;
    });

    afterEach(() => useContextStub.restore());

    // Reference equality is the point: JsonSerializer.deserializeFromInstance() always constructs a
    // new instance, so getting the exact same object back proves the default was never deserialized.
    it('should return the default, not the {} sentinel', () => details.should.equal(defaultDetails));
    it('should not run the default through deserialization', () => details.userId.value.should.equal('default'));
});
