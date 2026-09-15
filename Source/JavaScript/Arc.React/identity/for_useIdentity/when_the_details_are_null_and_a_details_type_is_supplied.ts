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

describe('when the details are null and a details type is supplied', () => {
    let useContextStub: sinon.SinonStub;
    let defaultDetails: UserDetails;
    let details: UserDetails;

    beforeEach(() => {
        defaultDetails = new UserDetails();
        defaultDetails.userId = new UserId('default');

        useContextStub = sinon.stub(React, 'useContext').returns({
            identity: { id: 'user', name: 'user', isSet: true, details: null, refresh: () => Promise.resolve() },
            detailsConstructor: undefined,
            isLoading: false,
            clearIdentity: () => { },
        });

        details = useIdentity(UserDetails, defaultDetails).details;
    });

    afterEach(() => useContextStub.restore());

    it('should use the default', () => details.should.equal(defaultDetails));
});
