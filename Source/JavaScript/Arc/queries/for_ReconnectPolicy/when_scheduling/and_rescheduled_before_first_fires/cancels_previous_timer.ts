// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { ReconnectPolicy } from '../../../ReconnectPolicy';

describe('when scheduling twice before the first timer fires', () => {
    let policy: ReconnectPolicy;
    let clock: sinon.SinonFakeTimers;
    let firstCallback: sinon.SinonStub;
    let secondCallback: sinon.SinonStub;

    beforeEach(() => {
        clock = sinon.useFakeTimers();
        policy = new ReconnectPolicy(100, 500, 500, 10_000);
        firstCallback = sinon.stub();
        secondCallback = sinon.stub();

        policy.schedule(firstCallback, 'first');
        // Schedule again before the first timer fires.
        policy.schedule(secondCallback, 'second');

        // Advance past both potential delays:
        // Attempt 1: min(500+500*1, 10_000) = 1000ms
        // Attempt 2: min(500+500*2, 10_000) = 1500ms
        clock.tick(2000);
    });

    afterEach(() => {
        clock.restore();
        sinon.restore();
    });

    it('should cancel the first timer so it never fires', () => firstCallback.called.should.be.false);
    it('should fire the second timer', () => secondCallback.calledOnce.should.be.true);
});
