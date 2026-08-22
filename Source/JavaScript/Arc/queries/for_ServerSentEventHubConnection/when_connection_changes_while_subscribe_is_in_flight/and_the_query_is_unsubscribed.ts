// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_deferred_promise } from '../given/a_deferred_promise';
import { a_server_sent_event_hub_connection } from '../given/a_server_sent_event_hub_connection';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';

const subscribeUrl = 'http://localhost/.cratis/queries/sse/subscribe';

describe('when a query is unsubscribed while its subscribe request is in flight and the connection changes', given(a_server_sent_event_hub_connection, context => {
    let clock: sinon.SinonFakeTimers;

    beforeEach(async () => {
        context.setup();
        clock = sinon.useFakeTimers({ toFake: ['setTimeout'] });
        sinon.stub(console, 'warn');
        const firstSubscribe = new a_deferred_promise<{ ok: boolean; status: number }>();
        context.fetchStub.onFirstCall().returns(firstSubscribe.promise);

        context.connection.subscribe('removed-query', { queryName: 'RemovedQuery' }, sinon.stub());
        context.simulateOpen();
        context.simulateMessage({ type: HubMessageType.Connected, payload: 'connection-a' });
        context.connection.subscribe('active-query', { queryName: 'ActiveQuery' }, sinon.stub());
        context.connection.unsubscribe('removed-query');
        context.simulateMessage({ type: HubMessageType.Connected, payload: 'connection-b' });

        firstSubscribe.resolve({ ok: false, status: 404 });
        await Promise.resolve();
        clock.tick(1000);
        await Promise.resolve();
    });

    afterEach(() => {
        clock.restore();
        sinon.restore();
    });

    it('should not subscribe the removed query on connection B', () => {
        const removedQueryCalls = context.fetchStub.getCalls()
            .filter(call => call.args[0] === subscribeUrl)
            .filter(call => (call.args[1].body as string).includes('"queryId":"removed-query"'));
        removedQueryCalls.length.should.equal(1);
        removedQueryCalls[0].args[1].body.should.equal(JSON.stringify({
            connectionId: 'connection-a',
            queryId: 'removed-query',
            request: { queryName: 'RemovedQuery' },
        }));
    });

    it('should subscribe the remaining query on connection B', () => {
        const activeQueryBodies = context.fetchStub.getCalls()
            .filter(call => call.args[0] === subscribeUrl)
            .map(call => call.args[1].body as string)
            .filter(body => body.includes('"queryId":"active-query"'));
        activeQueryBodies.should.deep.equal([
            JSON.stringify({ connectionId: 'connection-a', queryId: 'active-query', request: { queryName: 'ActiveQuery' } }),
            JSON.stringify({ connectionId: 'connection-b', queryId: 'active-query', request: { queryName: 'ActiveQuery' } }),
        ]);
    });

    it('should not reconnect', () => (context.policy.schedule as sinon.SinonStub).called.should.be.false);

    it('should retain only the remaining subscription', () => context.connection.queryCount.should.equal(1));
}));
