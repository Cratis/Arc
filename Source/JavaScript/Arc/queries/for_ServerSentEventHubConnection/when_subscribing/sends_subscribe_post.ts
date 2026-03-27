// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_server_sent_event_hub_connection } from '../given/a_server_sent_event_hub_connection';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';

describe('when subscribing sends the subscribe POST after Connected', given(a_server_sent_event_hub_connection, context => {
    const queryId = 'q1';
    const connectionId = 'conn-abc';

    beforeEach(() => {
        context.setup();
        context.connection.subscribe(queryId, { queryName: 'MyQuery' }, sinon.stub());
        context.simulateOpen();
        context.simulateMessage({ type: HubMessageType.Connected, payload: connectionId });
    });

    afterEach(() => sinon.restore());

    it('should POST to the subscribe URL', () => context.fetchStub.calledOnce.should.be.true);
    it('should pass the connection id in the request body', () => {
        const body = JSON.parse(context.fetchStub.getCall(0).args[1].body);
        body.connectionId.should.equal(connectionId);
    });
    it('should pass the query id in the request body', () => {
        const body = JSON.parse(context.fetchStub.getCall(0).args[1].body);
        body.queryId.should.equal(queryId);
    });
    it('should include credentials with the subscribe request', () => {
        context.fetchStub.getCall(0).args[1].credentials.should.equal('include');
    });

    describe('when a query result message arrives', () => {
        const result = { isSuccess: true, data: ['x'] };

        beforeEach(() => {
            context.simulateMessage({ type: HubMessageType.QueryResult, queryId, payload: result });
        });

        // The callback passed to subscribe during beforeEach is a stub — re-verify via query count
        it('should have one active subscription', () => context.connection.queryCount.should.equal(1));
    });
}));
