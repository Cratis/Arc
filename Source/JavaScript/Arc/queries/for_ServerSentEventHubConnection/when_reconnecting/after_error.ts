// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_server_sent_event_hub_connection } from '../given/a_server_sent_event_hub_connection';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';
import { ReconnectCallback } from '../../IReconnectPolicy';

describe('when connection errors', given(a_server_sent_event_hub_connection, context => {
    const queryId = 'q1';

    beforeEach(() => {
        context.setup();
        context.connection.subscribe(queryId, { queryName: 'MyQuery' }, sinon.stub());
        context.simulateOpen();
        context.simulateMessage({
            type: HubMessageType.Connected,
            payload: 'conn-1',
            supportsSubscriptionRevisions: true,
        });
        context.simulateError();
    });

    afterEach(() => sinon.restore());

    it('should close the EventSource', () => context.fakeEventSource.close.calledOnce.should.be.true);

    it('should delegate to the reconnect policy', () => {
        (context.policy.schedule as sinon.SinonStub).calledOnce.should.be.true;
    });

    describe('when the policy fires the reconnect callback', () => {
        let newEventSourceCreated: boolean;

        beforeEach(() => {
            newEventSourceCreated = false;
            // Replace the global EventSource with a stub that records construction.
            // SAFETY: This test double supplies the EventSource constructor and static ready-state shape used by the client.
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            const FakeClass = function (this: any) {
                newEventSourceCreated = true;
                return context.fakeEventSource;
            } as unknown as new (url: string) => EventSource;
            Object.assign(FakeClass, { CONNECTING: 0, OPEN: 1, CLOSED: 2 });
            (globalThis as Record<string, unknown>)['EventSource'] = FakeClass;

            const reconnectCallback = (context.policy.schedule as sinon.SinonStub).getCall(0).args[0] as ReconnectCallback;
            reconnectCallback();
        });

        it('should open a new EventSource', () => newEventSourceCreated.should.be.true);
    });

    describe('when Connected arrives after reconnect', () => {
        beforeEach(() => {
            const reconnectCallback = (context.policy.schedule as sinon.SinonStub).getCall(0).args[0] as ReconnectCallback;
            reconnectCallback();
            context.fetchStub.resetHistory();
            context.simulateMessage({ type: HubMessageType.Connected, payload: 'conn-2' });
        });

        it('should re-subscribe all active queries', () =>
            context.fetchStub.calledOnce.should.equal(true));

        it('should reset to legacy mode until the new connection advertises capabilities', () => {
            const rawBody = context.fetchStub.firstCall.args[1].body as string;
            let body: { revision?: number };
            try {
                body = JSON.parse(rawBody) as { revision?: number };
            } catch (error) {
                throw new Error('Expected a valid subscribe request body', {
                    cause: error,
                });
            }
            (body.revision === undefined).should.equal(true);
        });
    });
}));
