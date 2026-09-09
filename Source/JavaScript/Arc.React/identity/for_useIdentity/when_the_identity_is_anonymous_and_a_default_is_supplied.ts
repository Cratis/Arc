// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import sinon from 'sinon';
import { useIdentity } from '../useIdentity';

interface UserDetails {
    userId: string;
}

describe('when the identity is anonymous and a default is supplied', () => {
    let useContextStub: sinon.SinonStub;
    let defaultDetails: UserDetails;
    let details: UserDetails;

    beforeEach(() => {
        defaultDetails = { userId: 'default' };

        // isSet: false is the anonymous/not-yet-resolved sentinel - its `details` is `{}`, which is
        // not what should reach the caller when a default was supplied for exactly this situation.
        useContextStub = sinon.stub(React, 'useContext').returns({
            identity: { id: '', name: '', isSet: false, details: {}, refresh: () => Promise.resolve() },
            detailsConstructor: undefined,
            isLoading: false,
            clearIdentity: () => { },
        });

        details = useIdentity<UserDetails>(defaultDetails).details;
    });

    afterEach(() => useContextStub.restore());

    it('should return the default', () => details.should.equal(defaultDetails));
});
