// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_server_sent_event_hub_connection } from '../given/a_server_sent_event_hub_connection';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';
import { QueryResult } from '../../QueryResult';

describe(
    'when receiving unauthorized for a subscribed query',
    given(a_server_sent_event_hub_connection, (context) => {
        let callback: sinon.SinonStub;
        let receivedResult: QueryResult<unknown> | undefined;
        const queryId = 'q-auth-1';
        const connectionId = 'conn-123';

        beforeEach(() => {
            context.setup();
            callback = sinon.stub();

            context.connection.subscribe(queryId, { queryName: 'SecureQuery' }, callback);
            context.simulateOpen();

            // Server sends Connected message with the connection ID.
            context.simulateMessage({
                type: HubMessageType.Connected,
                payload: connectionId,
                supportsSubscriptionRevisions: true,
            });
            const body = getSubscribeBody();

            // Server replies with Unauthorized for this exact subscription.
            context.simulateMessage({
                type: HubMessageType.Unauthorized,
                queryId,
                revision: body.revision,
            });

            receivedResult = callback.firstCall?.args[0] as
                | QueryResult<unknown>
                | undefined;
        });

        afterEach(() => {
            sinon.restore();
        });

        function getSubscribeBody(): { revision: number } {
            try {
                return JSON.parse(context.fetchStub.firstCall.args[1].body as string) as {
                    revision: number;
                };
            } catch (error) {
                throw new Error('Expected a valid subscribe body', { cause: error });
            }
        }

        it('should invoke the subscriber callback', () => {
            callback.calledOnce.should.equal(true);
        });

        it('should report isAuthorized as false', () => {
            receivedResult!.isAuthorized.should.equal(false);
        });

        it('should report isSuccess as false', () => {
            receivedResult!.isSuccess.should.equal(false);
        });

        it('should remove the subscription', () => {
            context.connection.queryCount.should.equal(0);
        });
    }),
);
