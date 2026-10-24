// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { given } from '../../../given.js';
import { type HubMessage, HubMessageType } from '../../WebSocketHubConnection.js';
import { a_web_socket_hub_connection } from '../given/a_web_socket_hub_connection.js';

describe(
    'when the WebSocket server advertises subscription revision support',
    given(a_web_socket_hub_connection, (context) => {
        const queryId = 'query-1';
        let callback: sinon.SinonStub;
        let revision: number;

        beforeEach(() => {
            context.setup();
            callback = sinon.stub();
            context.connection.subscribe(queryId, { queryName: 'MyQuery' }, callback);
            context.simulateOpen(true);
            const upgradedRevision = sentMessage(0).revision;
            if (upgradedRevision === undefined) {
                throw new Error('Expected the subscription to carry a revision');
            }
            revision = upgradedRevision;

            context.simulateMessage({
                type: HubMessageType.QueryResult,
                queryId,
                payload: { data: ['legacy'] },
            });
            context.simulateMessage({
                type: HubMessageType.QueryResult,
                queryId,
                revision,
                payload: { data: ['current'] },
            });
        });

        afterEach(() => sinon.restore());

        it('should send the subscription once, with its revision, and not before the server has announced itself', () => {
            context.fakeSocket.send.calledOnce.should.equal(true);
            const message = sentMessage(0);
            message.type.should.equal(HubMessageType.Subscribe);
            (message.queryId === queryId).should.equal(true);
            revision.should.be.greaterThan(0);
        });

        it('should ignore a result without a revision and deliver the strict result once', () => {
            callback.calledOnce.should.equal(true);
            callback.firstCall.args[0].should.deep.equal({ data: ['current'] });
        });

        function sentMessage(index: number): HubMessage {
            try {
                return JSON.parse(
                    context.fakeSocket.send.getCall(index).args[0],
                ) as HubMessage;
            } catch (error) {
                throw new Error('Expected a valid hub message', { cause: error });
            }
        }
    }),
);
