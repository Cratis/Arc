// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { ReconnectPolicy } from '../../ReconnectPolicy';

describe('when resetting the reconnect policy', () => {
    let policy: ReconnectPolicy;
    let clock: sinon.SinonFakeTimers;
    let callbackStub: sinon.SinonStub;

    beforeEach(() => {
        clock = sinon.useFakeTimers();
        policy = new ReconnectPolicy(100, 500, 500, 10_000);
        callbackStub = sinon.stub();

        // Schedule a reconnect, then reset before the timer fires
        policy.schedule(callbackStub, 'url');
        policy.reset();
    });

    afterEach(() => {
        clock.restore();
        sinon.restore();
    });

    it('should reset the attempt counter to 0', () => policy.attempt.should.equal(0));

    it('should cancel the pending timer so the callback is never invoked', () => {
        clock.tick(60_000);
        callbackStub.callCount.should.equal(0);
    });

    it('should allow rescheduling from attempt 1 again', () => {
        const rescheduled = policy.schedule(callbackStub, 'url');
        rescheduled.should.be.true;
        policy.attempt.should.equal(1);
    });
});
