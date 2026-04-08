// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_server_sent_event_hub_connection } from '../given/a_server_sent_event_hub_connection';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';

describe('when unsubscribing the only query, nulls event source handlers before closing the event source', given(a_server_sent_event_hub_connection, context => {
    let handlersWereNullWhenClosed: { onopen: boolean; onmessage: boolean; onerror: boolean };

    beforeEach(async () => {
        context.setup();
        context.connection.subscribe('q1', { queryName: 'MyQuery' }, sinon.stub());
        context.simulateOpen();
        context.simulateMessage({ type: HubMessageType.Connected, payload: 'conn-abc' });

        handlersWereNullWhenClosed = { onopen: false, onmessage: false, onerror: false };
        context.fakeEventSource.close.callsFake(() => {
            handlersWereNullWhenClosed.onopen = context.fakeEventSource.onopen === null;
            handlersWereNullWhenClosed.onmessage = context.fakeEventSource.onmessage === null;
            handlersWereNullWhenClosed.onerror = context.fakeEventSource.onerror === null;
        });

        context.connection.unsubscribe('q1');

        // Allow the micro-task queue to drain so the async fetch completes.
        await Promise.resolve();
    });

    afterEach(() => sinon.restore());

    it('should have nulled onopen before calling eventSource.close()', () => handlersWereNullWhenClosed.onopen.should.be.true);
    it('should have nulled onmessage before calling eventSource.close()', () => handlersWereNullWhenClosed.onmessage.should.be.true);
    it('should have nulled onerror before calling eventSource.close()', () => handlersWereNullWhenClosed.onerror.should.be.true);
}));
