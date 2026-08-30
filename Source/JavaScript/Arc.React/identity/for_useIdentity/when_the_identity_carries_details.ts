// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import sinon from 'sinon';
import { useIdentity } from '../useIdentity';

interface UserDetails {
    login: string;
}

describe('when the identity carries details', () => {
    let useContextStub: sinon.SinonStub;
    let details: UserDetails;

    beforeEach(() => {
        useContextStub = sinon.stub(React, 'useContext').returns({
            identity: { id: 'user', name: 'user', isSet: true, details: { login: 'ada' }, refresh: () => Promise.resolve() },
            isLoading: false,
            clearIdentity: () => { },
        });

        details = useIdentity<UserDetails>({ login: '' }).details;
    });

    afterEach(() => useContextStub.restore());

    // The default stands in for missing details; it must never displace real ones.
    it('should keep what the identity resolved to', () => details.login.should.equal('ada'));
});
