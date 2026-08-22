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
        const body = parseBody(removedQueryCalls[0].args[1].body as string);
        body.connectionId.should.equal('connection-a');
        body.queryId.should.equal('removed-query');
        body.request.should.deep.equal({ queryName: 'RemovedQuery' });
        body.subscriptionGeneration.length.should.be.greaterThan(0);
    });

    it('should subscribe the remaining query on connection B', () => {
        const activeQueryBodies = context.fetchStub.getCalls()
            .filter(call => call.args[0] === subscribeUrl)
            .map(call => parseBody(call.args[1].body as string))
            .filter(body => body.queryId === 'active-query');
        activeQueryBodies.map(body => body.connectionId).should.deep.equal(['connection-a', 'connection-b']);
        activeQueryBodies[0].subscriptionGeneration.should.equal(activeQueryBodies[1].subscriptionGeneration);
    });

    it('should not reconnect', () => (context.policy.schedule as sinon.SinonStub).called.should.be.false);

    it('should retain only the remaining subscription', () => context.connection.queryCount.should.equal(1));

    function parseBody(rawBody: string): {
        connectionId: string;
        queryId: string;
        request: { queryName: string };
        subscriptionGeneration: string;
    } {
        try {
            return JSON.parse(rawBody) as ReturnType<typeof parseBody>;
        } catch (error) {
            throw new Error('Expected a valid subscribe request body', { cause: error });
        }
    }
}));
