// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { HubConnectionKeepAlive } from '../../HubConnectionKeepAlive';

describe('when reconfiguring a running keep-alive', () => {
    let clock: sinon.SinonFakeTimers;
    let onIdle: sinon.SinonStub;
    let keepAlive: HubConnectionKeepAlive;

    const originalIntervalMs = 500;
    const originalThresholdMs = 750;
    const newIntervalMs = 1000;
    const newThresholdMs = 2000;

    beforeEach(() => {
        clock = sinon.useFakeTimers();
        onIdle = sinon.stub();
        keepAlive = new HubConnectionKeepAlive(originalIntervalMs, onIdle, originalThresholdMs);
        keepAlive.start();
        keepAlive.reconfigure(newIntervalMs, newThresholdMs);
    });

    afterEach(() => {
        keepAlive.stop();
        clock.restore();
        sinon.restore();
    });

    it('should expose the new idle threshold', () => keepAlive.idleThresholdMs.should.equal(newThresholdMs));

    describe('and the original threshold elapses', () => {
        beforeEach(() => {
            // Well past the original 750ms threshold, but short of the new 2000ms one.
            clock.tick(originalThresholdMs + 1);
        });

        it('should not invoke the onIdle callback', () => onIdle.called.should.be.false);
    });

    describe('and the new threshold elapses', () => {
        beforeEach(() => {
            // Checks now fire every 1000ms; the tick at 2000ms sees 2000ms of inactivity.
            clock.tick(newThresholdMs + 1);
        });

        it('should invoke the onIdle callback', () => onIdle.calledOnce.should.be.true);
    });
});
