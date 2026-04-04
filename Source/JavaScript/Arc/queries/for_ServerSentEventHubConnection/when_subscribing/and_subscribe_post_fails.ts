// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_server_sent_event_hub_connection } from '../given/a_server_sent_event_hub_connection';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';

describe('when subscribe POST returns a non-OK status', given(a_server_sent_event_hub_connection, context => {
    beforeEach(async () => {
        context.setup();

        // Make the subscribe POST resolve with a 404 (connection not found on server).
        context.fetchStub.resolves({ ok: false, status: 404 });

        context.connection.subscribe('q1', { queryName: 'MyQuery' }, sinon.stub());
        context.simulateOpen();
        context.simulateMessage({ type: HubMessageType.Connected, payload: 'conn-abc' });

        // Allow the fetch promise chain (.then) to execute.
        await new Promise(resolve => setTimeout(resolve, 0));
    });

    afterEach(() => sinon.restore());

    it('should schedule a reconnect via the policy', () => {
        (context.policy.schedule as sinon.SinonStub).calledOnce.should.be.true;
    });
}));

describe('when subscribe POST rejects with a network error', given(a_server_sent_event_hub_connection, context => {
    beforeEach(async () => {
        context.setup();

        // Make the subscribe POST reject (network failure).
        context.fetchStub.rejects(new Error('Network error'));

        context.connection.subscribe('q1', { queryName: 'MyQuery' }, sinon.stub());
        context.simulateOpen();
        context.simulateMessage({ type: HubMessageType.Connected, payload: 'conn-abc' });

        // Allow the fetch promise chain (.catch) to execute.
        await new Promise(resolve => setTimeout(resolve, 0));
    });

    afterEach(() => sinon.restore());

    it('should schedule a reconnect via the policy', () => {
        (context.policy.schedule as sinon.SinonStub).calledOnce.should.be.true;
    });
}));
