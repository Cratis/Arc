// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_server_sent_event_hub_connection } from '../given/a_server_sent_event_hub_connection';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';

describe(
    'when subscribing sends the subscribe POST after Connected',
    given(a_server_sent_event_hub_connection, (context) => {
        const queryId = 'q1';
        const connectionId = 'conn-abc';
        let callback: sinon.SinonStub;

        beforeEach(() => {
            context.setup();
            callback = sinon.stub();
            context.connection.subscribe(queryId, { queryName: 'MyQuery' }, callback);
            context.simulateOpen();
            context.simulateMessage({
                type: HubMessageType.Connected,
                payload: connectionId,
                supportsSubscriptionRevisions: true,
            });
        });

        afterEach(() => sinon.restore());

        it('should POST to the subscribe URL', () =>
            context.fetchStub.calledOnce.should.be.true);
        it('should pass the connection id in the request body', () =>
            getBody().connectionId.should.equal(connectionId));
        it('should pass the query id in the request body', () =>
            getBody().queryId.should.equal(queryId));
        it('should pass a positive numeric revision in the request body', () =>
            getBody().revision.should.equal(1));

        function getBody(): {
            connectionId: string;
            queryId: string;
            revision: number;
        } {
            const rawBody = context.fetchStub.getCall(0).args[1].body as string;
            try {
                return JSON.parse(rawBody) as {
                    connectionId: string;
                    queryId: string;
                    revision: number;
                };
            } catch (error) {
                throw new Error('Expected a valid subscribe request body', {
                    cause: error,
                });
            }
        }

        describe('when a query result message arrives', () => {
            const result = { isSuccess: true, data: ['x'] };

            beforeEach(() => {
                context.simulateMessage({
                    type: HubMessageType.QueryResult,
                    queryId,
                    payload: { isSuccess: true, data: ['legacy'] },
                });
                context.simulateMessage({
                    type: HubMessageType.QueryResult,
                    queryId,
                    revision: getBody().revision,
                    payload: result,
                });
            });

            it('should ignore the missing-revision frame and deliver the exact revision once', () => {
                callback.calledOnce.should.equal(true);
                callback.firstCall.args[0].should.deep.equal(result);
            });
        });
    }),
);
