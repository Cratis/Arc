// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_web_socket_hub_connection } from '../given/a_web_socket_hub_connection';
import { given } from '../../../given';
import { type HubMessage, HubMessageType } from '../../WebSocketHubConnection';

describe(
    'when stale frames arrive after a WebSocket subscription is replaced',
    given(a_web_socket_hub_connection, (context) => {
        const queryId = 'query-a';
        let originalCallback: sinon.SinonStub;
        let replacementCallback: sinon.SinonStub;

        beforeEach(() => {
            context.setup();
            sinon.stub(console, 'warn');
            sinon.stub(console, 'error');
            originalCallback = sinon.stub();
            replacementCallback = sinon.stub();

            context.connection.subscribe(
                queryId,
                { queryName: 'Original' },
                originalCallback,
            );
            context.simulateOpen();
            context.simulateMessage({
                type: HubMessageType.Connected,
                supportsSubscriptionRevisions: true,
            });
            const originalRevision = sentMessage(1).revision!;

            context.connection.subscribe(
                queryId,
                { queryName: 'Replacement' },
                replacementCallback,
            );
            const replacementRevision = sentMessage(2).revision!;

            context.simulateMessage({
                type: HubMessageType.QueryResult,
                queryId,
                revision: originalRevision,
                payload: { data: ['stale'] },
            });
            context.simulateMessage({
                type: HubMessageType.Unauthorized,
                queryId,
                revision: originalRevision,
            });
            context.simulateMessage({
                type: HubMessageType.Error,
                queryId,
                revision: originalRevision,
                payload: 'stale',
            });
            context.simulateMessage({
                type: HubMessageType.QueryResult,
                queryId,
                revision: replacementRevision,
                payload: { data: ['current'] },
            });
        });

        afterEach(() => sinon.restore());

        it('should not deliver the stale result', () =>
            originalCallback.called.should.be.false);
        it('should deliver the replacement result', () =>
            replacementCallback.calledOnce.should.be.true);
        it('should retain the replacement after stale unauthorized', () =>
            context.connection.queryCount.should.equal(1));
        it('should not log the stale error', () =>
            (console.error as sinon.SinonStub).called.should.be.false);

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
