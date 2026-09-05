// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { given } from '../../../given';
import { type HubMessage, HubMessageType } from '../../WebSocketHubConnection';
import { a_web_socket_hub_connection } from '../given/a_web_socket_hub_connection';

describe(
    'when unsubscribing before the WebSocket server advertises revision support',
    given(a_web_socket_hub_connection, (context) => {
        const queryId = 'query-1';
        let message: HubMessage;

        beforeEach(() => {
            context.setup();
            context.connection.subscribe(queryId, { queryName: 'MyQuery' }, sinon.stub());
            context.simulateOpen();
            context.fakeSocket.send.resetHistory();
            context.connection.unsubscribe(queryId);
            message = sentMessage();
        });

        afterEach(() => sinon.restore());

        it('should send a legacy unsubscribe', () => {
            message.type.should.equal(HubMessageType.Unsubscribe);
            (message.queryId === queryId).should.equal(true);
            (message.revision === undefined).should.equal(true);
        });

        function sentMessage(): HubMessage {
            try {
                return JSON.parse(
                    context.fakeSocket.send.firstCall.args[0],
                ) as HubMessage;
            } catch (error) {
                throw new Error('Expected a valid unsubscribe message', {
                    cause: error,
                });
            }
        }
    }),
);
