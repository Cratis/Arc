// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_server_sent_event_hub_connection } from '../given/a_server_sent_event_hub_connection';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';

describe('when unsubscribing the only query', given(a_server_sent_event_hub_connection, context => {
    beforeEach(async () => {
        context.setup();
        context.connection.subscribe('q1', { queryName: 'MyQuery' }, sinon.stub());
        context.simulateOpen();
        context.simulateMessage({ type: HubMessageType.Connected, payload: 'conn-abc' });

        context.connection.unsubscribe('q1');

        // Allow the micro-task queue to drain so the async fetch completes.
        await Promise.resolve();
    });

    afterEach(() => sinon.restore());

    it('should close the event source', () => context.fakeEventSource.close.calledOnce.should.be.true);

    it('should send an unsubscribe POST request', () => {
        const unsubscribeCalls = context.fetchStub.args.filter(
            (args: unknown[]) => typeof args[0] === 'string' && (args[0] as string).includes('unsubscribe')
        );
        unsubscribeCalls.length.should.equal(1);
    });
}));
