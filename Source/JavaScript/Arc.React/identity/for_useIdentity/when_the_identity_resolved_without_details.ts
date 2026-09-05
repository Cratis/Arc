// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import sinon from 'sinon';
import { useIdentity } from '../useIdentity';

interface UserDetails {
    login: string;
}

describe('when the identity resolved without details', () => {
    let useContextStub: sinon.SinonStub;
    let details: UserDetails;

    beforeEach(() => {
        // An identity that came back from the endpoint - isSet is true - but carried nothing in
        // details. Callers dereference details unguarded because the return type promises it is
        // there, so a null here took a whole page down after the backend restarted.
        useContextStub = sinon.stub(React, 'useContext').returns({
            identity: { id: 'user', name: 'user', isSet: true, details: null, refresh: () => Promise.resolve() },
            isLoading: false,
            clearIdentity: () => { },
        });

        details = useIdentity<UserDetails>({ login: '' }).details;
    });

    afterEach(() => useContextStub.restore());

    it('should stand in with the supplied default', () => details.should.deep.equal({ login: '' }));

    it('should never hand back nothing', () => (details === null || details === undefined).should.be.false);
});
