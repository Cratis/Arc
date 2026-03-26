// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { ReconnectPolicy } from '../../../ReconnectPolicy';

describe('when scheduling and max attempts not reached', () => {
    let policy: ReconnectPolicy;
    let clock: sinon.SinonFakeTimers;
    let callbackStub: sinon.SinonStub;
    let scheduled: boolean;

    beforeEach(() => {
        clock = sinon.useFakeTimers();
        policy = new ReconnectPolicy(100, 500, 500, 10_000);
        callbackStub = sinon.stub();

        scheduled = policy.schedule(callbackStub, 'test-label');
    });

    afterEach(() => {
        clock.restore();
        sinon.restore();
    });

    it('should return true', () => scheduled.should.be.true);
    it('should increment the attempt counter to 1', () => policy.attempt.should.equal(1));
    it('should not invoke the callback immediately', () => callbackStub.callCount.should.equal(0));

    describe('when the delay elapses', () => {
        beforeEach(() => {
            // Attempt 1: delay = min(500 + 500*1, 10_000) = 1000ms
            clock.tick(1000);
        });

        it('should invoke the callback once', () => callbackStub.calledOnce.should.be.true);
    });
});
