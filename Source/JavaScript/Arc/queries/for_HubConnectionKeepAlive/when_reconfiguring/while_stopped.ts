// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { HubConnectionKeepAlive } from '../../HubConnectionKeepAlive';

describe('when reconfiguring a keep-alive that has not been started', () => {
    let clock: sinon.SinonFakeTimers;
    let onIdle: sinon.SinonStub;
    let keepAlive: HubConnectionKeepAlive;

    beforeEach(() => {
        clock = sinon.useFakeTimers();
        onIdle = sinon.stub();
        keepAlive = new HubConnectionKeepAlive(500, onIdle, 750);
        keepAlive.reconfigure(1000, 2000);
        clock.tick(10000);
    });

    afterEach(() => {
        keepAlive.stop();
        clock.restore();
        sinon.restore();
    });

    it('should not start the timer', () => onIdle.called.should.be.false);
    it('should still apply the new idle threshold', () => keepAlive.idleThresholdMs.should.equal(2000));
});
