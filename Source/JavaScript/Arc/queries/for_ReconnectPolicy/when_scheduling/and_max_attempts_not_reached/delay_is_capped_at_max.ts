// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { ReconnectPolicy } from '../../../ReconnectPolicy';

describe('when scheduling and delay is capped at max', () => {
    let policy: ReconnectPolicy;
    let clock: sinon.SinonFakeTimers;
    let callbackStub: sinon.SinonStub;

    beforeEach(() => {
        clock = sinon.useFakeTimers();
        // Set maxDelayMs to a low value to make the cap observable
        policy = new ReconnectPolicy(100, 500, 500, 2_000);
        callbackStub = sinon.stub();

        // Exhaust enough attempts to hit the cap: min(500 + 500*n, 2000) hits cap at n=3
        policy.schedule(callbackStub, 'url'); // attempt 1, delay=1000
        clock.tick(1000);
        policy.schedule(callbackStub, 'url'); // attempt 2, delay=1500
        clock.tick(1500);
        policy.schedule(callbackStub, 'url'); // attempt 3, delay=2000 (at cap)
        clock.tick(2000);
        callbackStub.reset();

        policy.schedule(callbackStub, 'url'); // attempt 4, delay should still be capped at 2000
    });

    afterEach(() => {
        clock.restore();
        sinon.restore();
    });

    it('should not fire before the cap delay', () => {
        clock.tick(1999);
        callbackStub.callCount.should.equal(0);
    });

    it('should fire exactly at the cap delay', () => {
        clock.tick(2000);
        callbackStub.calledOnce.should.be.true;
    });
});
