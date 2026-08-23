// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_web_socket_hub_connection } from '../given/a_web_socket_hub_connection';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';
import { QueryResult } from '../../QueryResult';

describe(
    'when receiving unauthorized for a subscribed query',
    given(a_web_socket_hub_connection, (context) => {
        let callback: sinon.SinonStub;
        let receivedResult: QueryResult<unknown> | undefined;
        const queryId = 'q-auth-1';

        beforeEach(() => {
            context.setup();
            callback = sinon.stub();

            context.connection.subscribe(queryId, { queryName: 'SecureQuery' }, callback);
            context.simulateOpen();
            context.simulateMessage({
                type: HubMessageType.Connected,
                supportsSubscriptionRevisions: true,
            });
            const subscribe = getSubscribeMessage();

            // Server replies with Unauthorized for this exact subscription.
            context.simulateMessage({
                type: HubMessageType.Unauthorized,
                queryId,
                revision: subscribe.revision,
            });

            receivedResult = callback.firstCall?.args[0] as
                | QueryResult<unknown>
                | undefined;
        });

        afterEach(() => {
            sinon.restore();
        });

        function getSubscribeMessage(): { revision: number } {
            try {
                return JSON.parse(context.fakeSocket.send.getCall(1).args[0]) as {
                    revision: number;
                };
            } catch (error) {
                throw new Error('Expected a valid subscribe message', { cause: error });
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
