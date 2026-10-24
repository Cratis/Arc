// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { Globals } from '../../../Globals.js';
import { createFetchHelper } from '../../../helpers/fetchHelper.js';
import { QueryTransportMethod } from '../../QueryTransportMethod.js';
import { QueryResult } from '../../QueryResult.js';
import { ObservableQuerySubscription } from '../../ObservableQuerySubscription.js';
import { getSharedMultiplexer, resetSharedMultiplexer } from '../../ObservableQueryMultiplexer.js';
import { an_observable_query_for } from '../given/an_observable_query_for.js';

for (const direct of [true, false]) {
    for (const transport of [QueryTransportMethod.WebSocket, QueryTransportMethod.ServerSentEvents]) {
        describe(`with server validation failing over ${direct ? 'direct' : 'hub'} ${transport}`, () => {
            let received: QueryResult<string>[];
            let subscription: ObservableQuerySubscription<string>;
            let originalDirect: boolean;
            let originalTransport: QueryTransportMethod;
            let originalWebSocket: typeof WebSocket;
            let originalEventSource: typeof EventSource;
            let originalEventSourceFactory: typeof Globals.eventSourceFactory;
            let fetchHelper: ReturnType<typeof createFetchHelper>;
            let stream: {
                readyState: number;
                onopen: (() => void) | null;
                onclose: (() => void) | null;
                onerror: (() => void) | null;
                onmessage: ((event: { data: string }) => void) | null;
                send: sinon.SinonStub;
                close: sinon.SinonStub;
            };
            const rejection = {
                data: null,
                isSuccess: false,
                isAuthorized: true,
                isValid: false,
                hasExceptions: false,
                validationResults: [{ severity: 2, message: 'Id is invalid.', members: ['id'], state: {} }],
                exceptionMessages: [],
                exceptionStackTrace: '',
                paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 }
            };

            const sendDirectRejection = () => {
                stream.onmessage?.({ data: JSON.stringify(rejection) });
                // A server closes the direct transport after the one rejection frame.
                stream.onclose?.();
                stream.onmessage?.({ data: JSON.stringify(rejection) });
            };

            const hubQueryId = (fetchStub: sinon.SinonStub): string => {
                if (transport === QueryTransportMethod.ServerSentEvents) {
                    stream.onmessage?.({ data: JSON.stringify({ type: 'Connected', payload: 'connection-1' }) });
                    return JSON.parse(fetchStub.firstCall.args[1].body).queryId;
                }
                // The server announces itself before any subscription is sent.
                stream.onmessage?.({ data: JSON.stringify({ type: 'Connected', payload: 'connection-1' }) });
                return JSON.parse(stream.send.firstCall.args[0]).queryId;
            };

            const sendHubRejection = (fetchStub: sinon.SinonStub) => {
                const queryId = hubQueryId(fetchStub);
                stream.onmessage?.({ data: JSON.stringify({ type: 'QueryResult', queryId, payload: rejection }) });
                // A terminal subscription must not receive a stale duplicate frame.
                stream.onmessage?.({ data: JSON.stringify({ type: 'QueryResult', queryId, payload: rejection }) });
            };

            beforeEach(() => {
                originalDirect = Globals.queryDirectMode;
                originalTransport = Globals.queryTransportMethod;
                originalWebSocket = globalThis.WebSocket;
                originalEventSource = globalThis.EventSource;
                originalEventSourceFactory = Globals.eventSourceFactory;
                resetSharedMultiplexer();
                Globals.queryDirectMode = direct;
                Globals.queryTransportMethod = transport;
                received = [];
                stream = {
                    readyState: 0,
                    onopen: null,
                    onclose: null,
                    onerror: null,
                    onmessage: null,
                    send: sinon.stub(),
                    close: sinon.stub()
                };
                const socketStub = sinon.stub().returns(stream);
                Object.assign(socketStub, { CONNECTING: 0, OPEN: 1, CLOSING: 2, CLOSED: 3 });
                (globalThis as Record<string, unknown>)['WebSocket'] = socketStub;
                (globalThis as Record<string, unknown>)['EventSource'] = Object.assign(sinon.stub().returns(stream), { OPEN: 1 });
                Globals.eventSourceFactory = () => stream as unknown as EventSource;
                fetchHelper = createFetchHelper();
                const fetchStub = fetchHelper.stubFetch().resolves({ ok: true } as Response);
                const query = new an_observable_query_for().query;
                query.setOrigin('https://example.com');
                subscription = query.subscribe(result => received.push(result), { id: 'invalid' });
                stream.readyState = 1;
                stream.onopen?.();

                if (direct) {
                    sendDirectRejection();
                } else {
                    sendHubRejection(fetchStub);
                }
            });

            afterEach(() => {
                subscription?.unsubscribe();
                resetSharedMultiplexer();
                Globals.queryDirectMode = originalDirect;
                Globals.queryTransportMethod = originalTransport;
                Globals.eventSourceFactory = originalEventSourceFactory;
                globalThis.WebSocket = originalWebSocket;
                globalThis.EventSource = originalEventSource;
                fetchHelper.restore();
            });

            it('should deliver one result to the subscriber', () => received.should.have.lengthOf(1));
            it('should be invalid', () => received[0].isValid.should.be.false);
            it('should not be successful', () => received[0].isSuccess.should.be.false);
            it('should preserve the validation results', () => received[0].validationResults.should.deep.equal(rejection.validationResults));
            it('should settle the rejected subscription', () => {
                if (direct) {
                    stream.close.should.have.been.calledOnce;
                } else {
                    getSharedMultiplexer()!.getConnectionsSnapshot().every(connection => connection.queryCount === 0).should.be.true;
                }
            });
        });
    }
}
