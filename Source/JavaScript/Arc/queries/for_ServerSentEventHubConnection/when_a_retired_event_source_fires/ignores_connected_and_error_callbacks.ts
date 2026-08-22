// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_server_sent_event_hub_connection } from '../given/a_server_sent_event_hub_connection';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';

describe('when a retired EventSource fires connected and error callbacks', given(a_server_sent_event_hub_connection, context => {
    let staleMessage: (event: MessageEvent) => void;
    let staleError: () => void;
    let currentSource: typeof context.fakeEventSource;

    beforeEach(() => {
        context.setup();
        sinon.stub(console, 'warn');
        context.connection.subscribe('q1', { queryName: 'MyQuery' }, sinon.stub());
        context.simulateOpen();
        context.simulateMessage({ type: HubMessageType.Connected, payload: 'connection-a' });

        const retiredSource = context.eventSources[0];
        staleMessage = retiredSource.onmessage!;
        staleError = retiredSource.onerror!;
        staleError();

        const reconnect = (context.policy.schedule as sinon.SinonStub).firstCall.args[0] as () => void;
        reconnect();
        currentSource = context.eventSources[1];
        context.simulateOpen();
        context.simulateMessage({ type: HubMessageType.Connected, payload: 'connection-b' });

        staleMessage({
            data: JSON.stringify({ type: HubMessageType.Connected, payload: 'stale-connection' }),
        } as MessageEvent);
        staleError();
    });

    afterEach(() => sinon.restore());

    it('should not send another subscription for stale connected', () => context.fetchStub.callCount.should.equal(2));
    it('should keep the current EventSource open', () => currentSource.close.called.should.be.false);
    it('should not schedule another reconnect for stale error', () => (context.policy.schedule as sinon.SinonStub).calledOnce.should.be.true);
}));
