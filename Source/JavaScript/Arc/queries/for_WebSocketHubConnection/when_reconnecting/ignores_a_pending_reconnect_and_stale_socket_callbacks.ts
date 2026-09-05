// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_web_socket_hub_connection } from '../given/a_web_socket_hub_connection';
import { given } from '../../../given';
import { type HubMessage, HubMessageType } from '../../WebSocketHubConnection';
import type { ReconnectCallback } from '../../IReconnectPolicy';

for (const supportsSubscriptionRevisions of [false, true]) {
    const mode = supportsSubscriptionRevisions ? 'revision-aware' : 'legacy';

    describe(`when an immediate ${mode} socket open wins during reconnect backoff`, given(a_web_socket_hub_connection, context => {
        const queryId = 'query-a';
        let callback: sinon.SinonStub;
        let secondSocket: typeof context.fakeSocket;

        beforeEach(() => {
            context.setup();
            sinon.stub(console, 'log');
            sinon.stub(console, 'warn');
            sinon.stub(console, 'error');
            callback = sinon.stub();

            context.connection.subscribe(queryId, { queryName: 'Original' }, sinon.stub());
            const firstSocket = context.fakeSocket;
            firstSocket.readyState = WebSocket.OPEN;
            firstSocket.onopen?.();
            if (supportsSubscriptionRevisions) {
                firstSocket.onmessage?.(messageEvent({
                    type: HubMessageType.Connected,
                    supportsSubscriptionRevisions: true,
                }));
            }

            const staleMessage = firstSocket.onmessage!;
            const staleClose = firstSocket.onclose!;
            const staleError = firstSocket.onerror!;
            firstSocket.readyState = WebSocket.CLOSED;
            staleClose();

            secondSocket = createSocket();
            context.WebSocketStub.onSecondCall().returns(secondSocket);
            context.connection.subscribe(queryId, { queryName: 'Replacement' }, callback);

            const reconnectCallback = (context.policy.schedule as sinon.SinonStub).firstCall.args[0] as ReconnectCallback;
            reconnectCallback();

            secondSocket.readyState = WebSocket.OPEN;
            secondSocket.onopen?.();
            if (supportsSubscriptionRevisions) {
                secondSocket.onmessage?.(messageEvent({
                    type: HubMessageType.Connected,
                    supportsSubscriptionRevisions: true,
                }));
            }

            const revision = supportsSubscriptionRevisions
                ? sentMessages(secondSocket).find(message => message.type === HubMessageType.Subscribe && message.revision !== undefined)!.revision
                : undefined;

            staleMessage(messageEvent({
                type: HubMessageType.Connected,
                supportsSubscriptionRevisions: true,
            }));
            staleMessage(messageEvent({
                type: HubMessageType.QueryResult,
                queryId,
                revision,
                payload: { data: ['stale'] },
            }));
            staleMessage(messageEvent({
                type: HubMessageType.Unauthorized,
                queryId,
                revision,
            }));
            staleMessage(messageEvent({
                type: HubMessageType.Error,
                queryId,
                revision,
                payload: 'stale',
            }));
            staleError({} as Event);
            staleClose();

            secondSocket.onmessage?.(messageEvent({
                type: HubMessageType.QueryResult,
                queryId,
                revision,
                payload: { data: ['current'] },
            }));
        });

        afterEach(() => {
            context.connection.dispose();
            sinon.restore();
        });

        it('should cancel the pending reconnect when the immediate open wins', () =>
            (context.policy.cancel as sinon.SinonStub).calledOnce.should.be.true);
        it('should not let the pending callback create a third socket', () =>
            context.WebSocketStub.callCount.should.equal(2));
        it('should not close the current socket', () => secondSocket.close.called.should.be.false);
        it('should not schedule another reconnect from the stale close', () =>
            (context.policy.schedule as sinon.SinonStub).calledOnce.should.be.true);
        it('should not process stale result or unauthorized frames', () => callback.calledOnce.should.be.true);
        it('should retain the current subscription', () => context.connection.queryCount.should.equal(1));
        it('should not process the stale error frame or callback', () =>
            (console.error as sinon.SinonStub).called.should.be.false);
    }));
}

function createSocket() {
    return {
        onopen: null as (() => void) | null,
        onclose: null as (() => void) | null,
        onerror: null as ((error: Event) => void) | null,
        onmessage: null as ((event: MessageEvent) => void) | null,
        send: sinon.stub(),
        close: sinon.stub(),
        readyState: WebSocket.CONNECTING as number,
    };
}

function messageEvent(message: HubMessage): MessageEvent {
    return { data: JSON.stringify(message) } as MessageEvent;
}

function sentMessages(socket: ReturnType<typeof createSocket>): HubMessage[] {
    return socket.send.args.map(args => JSON.parse(args[0]) as HubMessage);
}
