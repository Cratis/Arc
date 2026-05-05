// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_server_sent_event_hub_connection } from '../given/a_server_sent_event_hub_connection';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';

async function exhaustRetries(clock: sinon.SinonFakeTimers): Promise<void> {
    // Three retry delays: 200 ms (attempt 1), 400 ms (attempt 2), 600 ms (attempt 3).
    // Two microtask flushes are needed per tick: the first lets a rejected fetch propagate
    // through the .then() step (which passes it forward), and the second lets .catch() fire
    // and schedule the next setTimeout.  For resolved-but-not-ok responses a single flush
    // is enough, but two is harmless.
    for (const delay of [200, 400, 600]) {
        clock.tick(delay);
        await Promise.resolve();
        await Promise.resolve();
    }
}

describe('when subscribe POST returns a non-OK status', given(a_server_sent_event_hub_connection, context => {
    let clock: sinon.SinonFakeTimers;

    beforeEach(async () => {
        context.setup();
        clock = sinon.useFakeTimers({ toFake: ['setTimeout'] });

        context.fetchStub.resolves({ ok: false, status: 404 });

        context.connection.subscribe('q1', { queryName: 'MyQuery' }, sinon.stub());
        context.simulateOpen();
        context.simulateMessage({ type: HubMessageType.Connected, payload: 'conn-abc' });

        // Flush the initial fetch promise chain.
        await Promise.resolve();
    });

    afterEach(() => {
        clock.restore();
        sinon.restore();
    });

    it('should not immediately reconnect before retries are exhausted', () => {
        (context.policy.schedule as sinon.SinonStub).called.should.be.false;
    });

    it('should schedule a reconnect via the policy after exhausting all retries', async () => {
        await exhaustRetries(clock);
        (context.policy.schedule as sinon.SinonStub).calledOnce.should.be.true;
    });
}));

describe('when subscribe POST rejects with a network error', given(a_server_sent_event_hub_connection, context => {
    let clock: sinon.SinonFakeTimers;

    beforeEach(async () => {
        context.setup();
        clock = sinon.useFakeTimers({ toFake: ['setTimeout'] });

        context.fetchStub.rejects(new Error('Network error'));

        context.connection.subscribe('q1', { queryName: 'MyQuery' }, sinon.stub());
        context.simulateOpen();
        context.simulateMessage({ type: HubMessageType.Connected, payload: 'conn-abc' });

        // Flush the initial fetch promise chain.
        await Promise.resolve();
    });

    afterEach(() => {
        clock.restore();
        sinon.restore();
    });

    it('should not immediately reconnect before retries are exhausted', () => {
        (context.policy.schedule as sinon.SinonStub).called.should.be.false;
    });

    it('should schedule a reconnect via the policy after exhausting all retries', async () => {
        await exhaustRetries(clock);
        (context.policy.schedule as sinon.SinonStub).calledOnce.should.be.true;
    });
}));
