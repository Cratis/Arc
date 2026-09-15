// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import sinon from 'sinon';
import { useIdentity } from '../useIdentity';

describe('when the identity is anonymous with no default', () => {
    let useContextStub: sinon.SinonStub;
    let rawDetails: object;
    let details: object;

    beforeEach(() => {
        rawDetails = {};

        useContextStub = sinon.stub(React, 'useContext').returns({
            identity: { id: '', name: '', isSet: false, details: rawDetails, refresh: () => Promise.resolve() },
            detailsConstructor: undefined,
            isLoading: false,
            clearIdentity: () => { },
        });

        details = useIdentity().details;
    });

    afterEach(() => useContextStub.restore());

    // Pins today's behavior with no default to fall back to: the raw `{}` sentinel is handed back
    // unchanged. If this function ever needs to do something else here, this spec should fail and
    // force that to be a deliberate change, not an accidental side effect of something else.
    it('should return the raw details unchanged', () => details.should.equal(rawDetails));
});
