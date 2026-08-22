// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_deferred_promise } from '../given/a_deferred_promise';
import { a_server_sent_event_hub_connection } from '../given/a_server_sent_event_hub_connection';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';

describe(
    'when a query is replaced while its subscribe request is in flight and the connection changes',
    given(a_server_sent_event_hub_connection, (context) => {
        const queryId = 'q1';
        const replacementRequest = { queryName: 'ReplacementQuery' };
        let clock: sinon.SinonFakeTimers;
        let originalCallback: sinon.SinonStub;
        let replacementCallback: sinon.SinonStub;

        beforeEach(async () => {
            context.setup();
            clock = sinon.useFakeTimers({ toFake: ['setTimeout'] });
            sinon.stub(console, 'warn');
            const firstSubscribe = new a_deferred_promise<{
                ok: boolean;
                status: number;
            }>();
            context.fetchStub.onFirstCall().returns(firstSubscribe.promise);
            originalCallback = sinon.stub();
            replacementCallback = sinon.stub();

            context.connection.subscribe(
                queryId,
                { queryName: 'OriginalQuery' },
                originalCallback,
            );
            context.simulateOpen();
            context.simulateMessage({
                type: HubMessageType.Connected,
                payload: 'connection-a',
                supportsSubscriptionRevisions: true,
            });
            context.connection.subscribe(
                queryId,
                replacementRequest,
                replacementCallback,
            );
            context.simulateMessage({
                type: HubMessageType.Connected,
                payload: 'connection-b',
                supportsSubscriptionRevisions: true,
            });

            firstSubscribe.resolve({ ok: false, status: 404 });
            await Promise.resolve();
            clock.tick(1000);
            await Promise.resolve();
            context.simulateMessage({
                type: HubMessageType.QueryResult,
                queryId,
                revision: getSubscribeBody(2).revision,
                payload: { isSuccess: true },
            });
        });

        afterEach(() => {
            clock.restore();
            sinon.restore();
        });

        it('should send only the replacement on connection B', () => {
            const body = getSubscribeBody(2);
            body.connectionId.should.equal('connection-b');
            body.queryId.should.equal(queryId);
            body.request.should.deep.equal(replacementRequest);
            body.revision.should.be.greaterThan(0);
        });

        it('should not retry the obsolete subscription', () =>
            context.fetchStub.callCount.should.equal(3));

        it('should notify only the replacement callback', () => {
            originalCallback.called.should.equal(false);
            replacementCallback.calledOnce.should.equal(true);
        });

        it('should not reconnect', () =>
            (context.policy.schedule as sinon.SinonStub).called.should.be.false);

        it('should retain one active subscription', () =>
            context.connection.queryCount.should.equal(1));

        function getSubscribeBody(callIndex: number): {
            connectionId: string;
            queryId: string;
            request: object;
            revision: number;
        } {
            const rawBody = context.fetchStub.getCall(callIndex).args[1].body as string;
            try {
                return JSON.parse(rawBody) as {
                    connectionId: string;
                    queryId: string;
                    request: object;
                    revision: number;
                };
            } catch (error) {
                throw new Error('Expected a valid subscribe request body', {
                    cause: error,
                });
            }
        }
    }),
);
