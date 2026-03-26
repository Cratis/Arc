// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { HubConnectionKeepAlive } from '../HubConnectionKeepAlive';

describe('for HubConnectionKeepAlive', () => {
    let clock: sinon.SinonFakeTimers;

    beforeEach(() => { clock = sinon.useFakeTimers(); });
    afterEach(() => { clock.restore(); sinon.restore(); });

    describe('when started and the interval elapses without any activity', () => {
        let onIdle: sinon.SinonStub;
        let keepAlive: HubConnectionKeepAlive;

        beforeEach(() => {
            onIdle = sinon.stub();
            keepAlive = new HubConnectionKeepAlive(1000, onIdle);
            keepAlive.start();
            clock.tick(1001);
        });

        it('should invoke the onIdle callback', () => onIdle.calledOnce.should.be.true);
    });

    describe('when activity is recorded before the interval elapses', () => {
        let onIdle: sinon.SinonStub;
        let keepAlive: HubConnectionKeepAlive;

        beforeEach(() => {
            onIdle = sinon.stub();
            keepAlive = new HubConnectionKeepAlive(1000, onIdle);
            keepAlive.start();
            clock.tick(500);
            keepAlive.recordActivity();
            clock.tick(600);
        });

        it('should not invoke the onIdle callback', () => onIdle.called.should.be.false);
    });

    describe('when stopped before the interval elapses', () => {
        let onIdle: sinon.SinonStub;
        let keepAlive: HubConnectionKeepAlive;

        beforeEach(() => {
            onIdle = sinon.stub();
            keepAlive = new HubConnectionKeepAlive(1000, onIdle);
            keepAlive.start();
            clock.tick(500);
            keepAlive.stop();
            clock.tick(600);
        });

        it('should not invoke the onIdle callback', () => onIdle.called.should.be.false);
    });

    describe('when started twice', () => {
        let onIdle: sinon.SinonStub;
        let keepAlive: HubConnectionKeepAlive;

        beforeEach(() => {
            onIdle = sinon.stub();
            keepAlive = new HubConnectionKeepAlive(1000, onIdle);
            keepAlive.start();
            keepAlive.start();
            clock.tick(1001);
        });

        it('should invoke the onIdle callback exactly once', () => onIdle.calledOnce.should.be.true);
    });
});
