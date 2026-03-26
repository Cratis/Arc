// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_server_sent_event_hub_connection } from '../given/a_server_sent_event_hub_connection';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';

describe('when subscribing before Connected arrives', given(a_server_sent_event_hub_connection, context => {
    const queryId = 'q1';

    beforeEach(() => {
        // Subscribe before Connected message — no connectionId yet
        context.setup();
        context.connection.subscribe(queryId, { queryName: 'MyQuery' }, sinon.stub());
        context.simulateOpen();
        // No Connected message yet
    });

    afterEach(() => sinon.restore());

    it('should not POST before the Connected message arrives', () => context.fetchStub.callCount.should.equal(0));

    describe('when Connected message arrives', () => {
        beforeEach(() => {
            context.simulateMessage({ type: HubMessageType.Connected, payload: 'conn-1' });
        });

        it('should drain the pending subscription with a POST', () => context.fetchStub.calledOnce.should.be.true);
    });
}));
