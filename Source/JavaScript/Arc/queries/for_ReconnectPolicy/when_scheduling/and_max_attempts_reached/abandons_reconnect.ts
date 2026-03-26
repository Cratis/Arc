// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { ReconnectPolicy } from '../../../ReconnectPolicy';

describe('when scheduling and max attempts reached', () => {
    let policy: ReconnectPolicy;
    let clock: sinon.SinonFakeTimers;
    let callbackStub: sinon.SinonStub;
    let scheduled: boolean;

    beforeEach(() => {
        clock = sinon.useFakeTimers();
        // Use maxAttempts=2 so we can exhaust quickly
        policy = new ReconnectPolicy(2, 500, 500, 10_000);
        callbackStub = sinon.stub();

        policy.schedule(callbackStub, 'url'); // attempt 1
        clock.tick(1000);
        policy.schedule(callbackStub, 'url'); // attempt 2
        clock.tick(1500);
        callbackStub.reset();

        // This is the 3rd call — attempt >= maxAttempts (2), should not schedule
        scheduled = policy.schedule(callbackStub, 'url');
    });

    afterEach(() => {
        clock.restore();
        sinon.restore();
    });

    it('should return false', () => scheduled.should.be.false);
    it('should not invoke the callback', () => {
        clock.tick(60_000);
        callbackStub.callCount.should.equal(0);
    });
});
