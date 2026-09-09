// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import sinon from 'sinon';
import { useIdentity } from '../useIdentity';

// Deliberately undecorated - no @field members. Deserializing into this would construct
// `new UndecoratedDetails()` and copy nothing, discarding whatever the server sent.
class UndecoratedDetails {
    userId!: string;
}

describe('when the details type has no field metadata', () => {
    let useContextStub: sinon.SinonStub;
    let consoleWarnStub: sinon.SinonStub;
    let details: UndecoratedDetails;

    beforeEach(() => {
        consoleWarnStub = sinon.stub(console, 'warn');

        useContextStub = sinon.stub(React, 'useContext').returns({
            identity: { id: 'user', name: 'user', isSet: true, details: { userId: 'u-1' }, refresh: () => Promise.resolve() },
            detailsConstructor: undefined,
            isLoading: false,
            clearIdentity: () => { },
        });

        details = useIdentity(UndecoratedDetails).details;
    });

    afterEach(() => {
        useContextStub.restore();
        consoleWarnStub.restore();
    });

    it('should hand back the payload untouched', () => (details as unknown as { userId: string }).userId.should.equal('u-1'));
    it('should not blank the details', () => details.should.not.be.instanceOf(UndecoratedDetails));
    it('should warn about the missing @field decorators', () => consoleWarnStub.calledOnce.should.be.true);
});
