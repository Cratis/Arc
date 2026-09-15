// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import sinon from 'sinon';
import { useIdentity } from '../useIdentity';

interface UserDetails {
    userId: string;
}

describe('when no details type is supplied', () => {
    let useContextStub: sinon.SinonStub;
    let rawDetails: UserDetails;
    let details: UserDetails;

    beforeEach(() => {
        rawDetails = { userId: 'u-1' };

        useContextStub = sinon.stub(React, 'useContext').returns({
            identity: { id: 'user', name: 'user', isSet: true, details: rawDetails, refresh: () => Promise.resolve() },
            detailsConstructor: undefined,
            isLoading: false,
            clearIdentity: () => { },
        });

        details = useIdentity<UserDetails>({ userId: 'unknown' }).details;
    });

    afterEach(() => useContextStub.restore());

    // No type was passed, so the single-argument overload never attempts deserialization - the raw
    // payload object is handed back exactly as the provider produced it.
    it('should hand back the raw payload untouched', () => details.should.equal(rawDetails));
    it('should keep the raw payload a plain value, not a deserialized instance', () => details.userId.should.equal('u-1'));
});
