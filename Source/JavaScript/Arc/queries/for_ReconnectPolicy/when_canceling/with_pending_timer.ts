// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { ReconnectPolicy } from '../../ReconnectPolicy';

describe('when canceling a pending reconnect', () => {
    let policy: ReconnectPolicy;
    let clock: sinon.SinonFakeTimers;
    let callbackStub: sinon.SinonStub;

    beforeEach(() => {
        clock = sinon.useFakeTimers();
        policy = new ReconnectPolicy(100, 500, 500, 10_000);
        callbackStub = sinon.stub();

        // Schedule a reconnect, then cancel before the timer fires
        policy.schedule(callbackStub, 'url');
        policy.cancel();
    });

    afterEach(() => {
        clock.restore();
        sinon.restore();
    });

    it('should not invoke the callback after the timer would have fired', () => {
        clock.tick(60_000);
        callbackStub.callCount.should.equal(0);
    });

    it('should preserve the attempt counter', () => policy.attempt.should.equal(1));
});
