// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { given } from '../../../given';
import { type HubMessage, HubMessageType } from '../../WebSocketHubConnection';
import { a_web_socket_hub_connection } from '../given/a_web_socket_hub_connection';

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
            context.simulateOpen();

            context.simulateMessage({
                type: HubMessageType.Connected,
                supportsSubscriptionRevisions: true,
            });
            const upgradedRevision = sentMessage(1).revision;
            if (upgradedRevision === undefined) {
                throw new Error('Expected the upgraded subscription to carry a revision');
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

        it('should initially subscribe without a revision for older servers', () =>
            (sentMessage(0).revision === undefined).should.equal(true));

        it('should replace the legacy subscription with its current revision', () => {
            const message = sentMessage(1);
            message.type.should.equal(HubMessageType.Subscribe);
            (message.queryId === queryId).should.equal(true);
            revision.should.be.greaterThan(0);
        });

        it('should ignore the temporary legacy result and deliver the strict result once', () => {
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
