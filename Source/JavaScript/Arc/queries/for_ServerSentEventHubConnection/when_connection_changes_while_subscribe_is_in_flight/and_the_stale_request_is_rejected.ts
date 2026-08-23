// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_deferred_promise } from '../given/a_deferred_promise';
import { a_server_sent_event_hub_connection } from '../given/a_server_sent_event_hub_connection';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';

describe(
    'when the connection changes while a subscribe request is in flight and the stale request is rejected',
    given(a_server_sent_event_hub_connection, (context) => {
        const queryId = 'q1';
        const request = { queryName: 'MyQuery' };
        let clock: sinon.SinonFakeTimers;

        beforeEach(async () => {
            context.setup();
            clock = sinon.useFakeTimers({ toFake: ['setTimeout'] });
            sinon.stub(console, 'warn');
            const firstSubscribe = new a_deferred_promise<{ ok: boolean }>();
            context.fetchStub.onFirstCall().returns(firstSubscribe.promise);

            context.connection.subscribe(queryId, request, sinon.stub());
            context.simulateOpen();
            context.simulateMessage({
                type: HubMessageType.Connected,
                payload: 'connection-a',
                supportsSubscriptionRevisions: true,
            });
            context.simulateMessage({
                type: HubMessageType.Connected,
                payload: 'connection-b',
                supportsSubscriptionRevisions: true,
            });

            firstSubscribe.reject(new Error('Connection A failed'));
            await Promise.resolve();
            await Promise.resolve();
            clock.tick(1000);
            await Promise.resolve();
        });

        afterEach(() => {
            clock.restore();
            sinon.restore();
        });

        it('should send the active subscription on connection B', () => {
            const body = parseBody(context.fetchStub.getCall(1).args[1].body as string);
            body.connectionId.should.equal('connection-b');
            body.queryId.should.equal(queryId);
            body.request.should.deep.equal(request);
            body.revision.should.be.greaterThan(0);
        });

        it('should not retry the obsolete connection A request', () =>
            context.fetchStub.callCount.should.equal(2));

        it('should not reconnect', () =>
            (context.policy.schedule as sinon.SinonStub).called.should.be.false);

        it('should retain the active subscription', () =>
            context.connection.queryCount.should.equal(1));

        function parseBody(rawBody: string): {
            connectionId: string;
            queryId: string;
            request: object;
            revision: number;
        } {
            try {
                return JSON.parse(rawBody) as ReturnType<typeof parseBody>;
            } catch (error) {
                throw new Error('Expected a valid subscribe request body', {
                    cause: error,
                });
            }
        }
    }),
);
