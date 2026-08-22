// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { given } from '../../../given';
import { Globals } from '../../../Globals';
import { HubMessageType } from '../../WebSocketHubConnection';
import type { ObservableQuerySubscription } from '../../ObservableQuerySubscription';
import { resetSharedMultiplexer } from '../../ObservableQueryMultiplexer';
import { QueryTransportMethod } from '../../QueryTransportMethod';
import { an_observable_query_for } from '../given/an_observable_query_for';

describe(
    'when subscribing with web socket hub payload without readiness',
    given(an_observable_query_for, (context) => {
        let callback: sinon.SinonStub;
        let subscription: ObservableQuerySubscription<string>;
        let originalQueryDirectMode: boolean;
        let originalTransportMethod: QueryTransportMethod;
        let originalWebSocket: typeof WebSocket;
        let socket: {
            readyState: number;
            onopen: (() => void) | null;
            onclose: (() => void) | null;
            onerror: ((event: Event) => void) | null;
            onmessage: ((event: { data: string }) => void) | null;
            send: sinon.SinonStub;
            close: sinon.SinonStub;
        };

        beforeEach(() => {
            originalQueryDirectMode = Globals.queryDirectMode;
            originalTransportMethod = Globals.queryTransportMethod;
            originalWebSocket = globalThis.WebSocket;
            resetSharedMultiplexer();

            Globals.queryDirectMode = false;
            Globals.queryTransportMethod = QueryTransportMethod.WebSocket;
            context.query.setOrigin('https://example.com');
            callback = sinon.stub();

            socket = {
                readyState: 0,
                onopen: null,
                onclose: null,
                onerror: null,
                onmessage: null,
                send: sinon.stub(),
                close: sinon.stub(),
            };

            const WebSocketStub = sinon.stub().returns(socket);
            Object.assign(WebSocketStub, {
                CONNECTING: 0,
                OPEN: 1,
                CLOSING: 2,
                CLOSED: 3,
            });
            (globalThis as Record<string, unknown>)['WebSocket'] = WebSocketStub;

            subscription = context.query.subscribe(callback, { id: 'test-id' });
            socket.readyState = 1;
            socket.onopen?.();

            let subscribeMessage: { queryId: string };
            try {
                subscribeMessage = JSON.parse(socket.send.firstCall.args[0]);
            } catch (error) {
                throw new Error('The WebSocket subscribe message was not valid JSON', {
                    cause: error,
                });
            }
            socket.onmessage?.({
                data: JSON.stringify({
                    type: HubMessageType.QueryResult,
                    queryId: subscribeMessage.queryId,
                    payload: {
                        data: 'ready result',
                        isSuccess: true,
                        isAuthorized: true,
                        isValid: true,
                        hasExceptions: false,
                        validationResults: [],
                        exceptionMessages: [],
                        exceptionStackTrace: '',
                        paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 },
                    },
                }),
            });
        });

        afterEach(() => {
            subscription?.unsubscribe();
            Globals.queryDirectMode = originalQueryDirectMode;
            Globals.queryTransportMethod = originalTransportMethod;
            globalThis.WebSocket = originalWebSocket;
            resetSharedMultiplexer();
            sinon.restore();
        });

        it('should normalize omitted readiness to true', () => {
            callback.firstCall.args[0].isReady.should.equal(true);
        });
    }),
);
