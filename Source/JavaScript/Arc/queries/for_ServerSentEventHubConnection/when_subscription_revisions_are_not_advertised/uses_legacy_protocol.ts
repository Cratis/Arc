// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';
import { a_server_sent_event_hub_connection } from '../given/a_server_sent_event_hub_connection';

describe(
    'when the SSE server does not advertise subscription revision support',
    given(a_server_sent_event_hub_connection, (context) => {
        const queryId = 'query-1';
        let callback: sinon.SinonStub;

        beforeEach(() => {
            context.setup();
            callback = sinon.stub();
            context.connection.subscribe(queryId, { queryName: 'MyQuery' }, callback);
            context.simulateOpen();
            context.simulateMessage({
                type: HubMessageType.Connected,
                payload: 'legacy-connection',
            });
            context.simulateMessage({
                type: HubMessageType.QueryResult,
                queryId,
                payload: { data: ['legacy'] },
            });
            context.connection.unsubscribe(queryId);
        });

        afterEach(() => sinon.restore());

        it('should omit the revision from the subscribe request', () =>
            (subscribeBody().revision === undefined).should.equal(true));

        it('should accept a missing-revision result frame', () => {
            callback.calledOnce.should.equal(true);
            callback.firstCall.args[0].should.deep.equal({ data: ['legacy'] });
        });

        it('should omit the revision from the unsubscribe request', () =>
            (unsubscribeBody().revision === undefined).should.equal(true));

        function subscribeBody(): { revision?: number } {
            return parseBody(context.fetchStub.firstCall.args[1].body as string);
        }

        function unsubscribeBody(): { revision?: number } {
            const unsubscribeCall = context.fetchStub
                .getCalls()
                .find((call) => (call.args[0] as string).includes('unsubscribe'));
            if (!unsubscribeCall) {
                throw new Error('Expected an unsubscribe request');
            }
            return parseBody(unsubscribeCall.args[1].body as string);
        }

        function parseBody(rawBody: string): { revision?: number } {
            try {
                return JSON.parse(rawBody) as { revision?: number };
            } catch (error) {
                throw new Error('Expected a valid subscription request body', {
                    cause: error,
                });
            }
        }
    }),
);
