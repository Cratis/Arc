// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { HubConnectionKeepAlive } from '../../HubConnectionKeepAlive';

describe('when started with a custom idle threshold larger than the check interval', () => {
    let clock: sinon.SinonFakeTimers;
    let onIdle: sinon.SinonStub;
    let keepAlive: HubConnectionKeepAlive;

    const checkIntervalMs = 500;
    const idleThresholdMs = 750;

    beforeEach(() => {
        clock = sinon.useFakeTimers();
        onIdle = sinon.stub();
        keepAlive = new HubConnectionKeepAlive(checkIntervalMs, onIdle, idleThresholdMs);
        keepAlive.start();
    });

    afterEach(() => {
        keepAlive.stop();
        clock.restore();
        sinon.restore();
    });

    describe('and the check interval elapses but idle threshold has not', () => {
        beforeEach(() => {
            // Advance past the check interval (500ms) but not the idle threshold (750ms).
            clock.tick(checkIntervalMs + 1);
        });

        it('should not invoke the onIdle callback', () => onIdle.called.should.be.false);
    });

    describe('and the idle threshold elapses', () => {
        beforeEach(() => {
            // Advance past the idle threshold. The second interval tick at 1000ms
            // sees 1000ms of inactivity which exceeds the 750ms threshold.
            clock.tick(checkIntervalMs * 2 + 1);
        });

        it('should invoke the onIdle callback', () => onIdle.calledOnce.should.be.true);
    });
});
