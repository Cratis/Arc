// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_web_socket_hub_connection } from '../given/a_web_socket_hub_connection.js';
import { given } from '../../../given.js';
import { HubMessageType } from '../../WebSocketHubConnection.js';
import { QueryResult } from '../../QueryResult.js';

for (const supportsRevisions of [false, true]) {
    describe(
        `when receiving a WebSocket query error with revision support ${supportsRevisions}`,
        given(a_web_socket_hub_connection, (context) => {
            const queryId = 'query-failed';
            const failure = 'MongoDB is unreachable.';
            let callback: sinon.SinonStub;
            let otherCallback: sinon.SinonStub;
            let result: QueryResult<unknown>;
            let revision: number | undefined;

            beforeEach(() => {
                context.setup();
                sinon.stub(console, 'error');
                callback = sinon.stub();
                otherCallback = sinon.stub();
                context.connection.subscribe(queryId, { queryName: 'Authors' }, callback);
                context.simulateOpen();
                context.simulateMessage({
                    type: HubMessageType.Connected,
                    supportsSubscriptionRevisions: supportsRevisions,
                });
                try {
                    revision = (JSON.parse(context.fakeSocket.send.lastCall.args[0] as string) as { revision?: number }).revision;
                } catch (error) {
                    throw new Error('Expected a valid subscribe message', { cause: error });
                }
                context.connection.subscribe('other-query', { queryName: 'Other' }, otherCallback);

                context.simulateMessage({ type: HubMessageType.Error, queryId, revision, payload: failure });

                result = callback.firstCall?.args[0] as QueryResult<unknown>;
            });

            afterEach(() => {
                context.connection.dispose();
                sinon.restore();
            });

            it('should deliver one QueryResult to the subscriber', () => {
                callback.calledOnce.should.be.true;
                result.should.be.instanceOf(QueryResult);
            });
            it('should report a ready failed result', () => {
                result.isReady.should.be.true;
                result.isSuccess.should.be.false;
                result.hasExceptions.should.be.true;
            });
            it('should carry the server error message', () =>
                result.exceptionMessages.should.deep.equal([failure]));
            it('should not report a validation or authorization rejection', () => {
                result.isValid.should.be.true;
                result.isAuthorized.should.be.true;
            });
            it('should have no result data', () => result.hasData.should.be.false);
            it('should not notify another query', () => otherCallback.called.should.be.false);
            it('should retain subscriptions for later results', () => {
                context.connection.queryCount.should.equal(2);
                context.simulateMessage({
                    type: HubMessageType.QueryResult,
                    queryId,
                    revision,
                    payload: { isSuccess: true, data: ['recovered'] },
                });
                callback.calledTwice.should.be.true;
                callback.secondCall.args[0].isSuccess.should.be.true;
            });
        }),
    );
}
