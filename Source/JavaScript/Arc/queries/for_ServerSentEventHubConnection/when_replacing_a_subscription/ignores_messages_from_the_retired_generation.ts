// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_server_sent_event_hub_connection } from '../given/a_server_sent_event_hub_connection';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';

describe('when messages arrive from a retired subscription generation', given(a_server_sent_event_hub_connection, context => {
    const queryId = 'q1';
    let originalCallback: sinon.SinonStub;
    let replacementCallback: sinon.SinonStub;
    let originalGeneration: string;
    let replacementGeneration: string;

    beforeEach(() => {
        context.setup();
        sinon.stub(console, 'warn');
        sinon.stub(console, 'error');
        originalCallback = sinon.stub();
        replacementCallback = sinon.stub();

        context.connection.subscribe(queryId, { queryName: 'OriginalQuery' }, originalCallback);
        context.simulateOpen();
        context.simulateMessage({ type: HubMessageType.Connected, payload: 'connection-a' });
        originalGeneration = getGeneration(0);

        context.connection.subscribe(queryId, { queryName: 'ReplacementQuery' }, replacementCallback);
        replacementGeneration = getGeneration(1);

        context.simulateMessage({
            type: HubMessageType.QueryResult,
            queryId,
            subscriptionGeneration: originalGeneration,
            payload: { isSuccess: true, data: ['stale'] },
        });
        context.simulateMessage({
            type: HubMessageType.Unauthorized,
            queryId,
            subscriptionGeneration: originalGeneration,
        });
        context.simulateMessage({
            type: HubMessageType.Error,
            queryId,
            subscriptionGeneration: originalGeneration,
            payload: 'stale error',
        });
        context.simulateMessage({
            type: HubMessageType.QueryResult,
            queryId,
            subscriptionGeneration: replacementGeneration,
            payload: { isSuccess: true, data: ['current'] },
        });
    });

    afterEach(() => sinon.restore());

    it('should not notify the retired callback', () => originalCallback.called.should.be.false);
    it('should deliver the current generation result', () => replacementCallback.calledOnce.should.be.true);
    it('should retain the replacement after stale unauthorized', () => context.connection.queryCount.should.equal(1));
    it('should not log the stale server error', () => (console.error as sinon.SinonStub).called.should.be.false);

    function getGeneration(callIndex: number): string {
        const rawBody = context.fetchStub.getCall(callIndex).args[1].body as string;
        try {
            return (JSON.parse(rawBody) as { subscriptionGeneration: string }).subscriptionGeneration;
        } catch (error) {
            throw new Error('Expected a valid subscribe request body', { cause: error });
        }
    }
}));
