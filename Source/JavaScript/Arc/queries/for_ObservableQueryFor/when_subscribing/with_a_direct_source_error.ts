// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { given } from '../../../given.js';
import { Globals } from '../../../Globals.js';
import { QueryTransportMethod } from '../../QueryTransportMethod.js';
import { ObservableQuerySubscription } from '../../ObservableQuerySubscription.js';
import { an_observable_query_for } from '../given/an_observable_query_for.js';

for (const transport of [QueryTransportMethod.WebSocket, QueryTransportMethod.ServerSentEvents]) {
    describe(`when subscribing with a direct source error over ${transport}`, given(an_observable_query_for, context => {
        let callback: sinon.SinonStub;
        let subscription: ObservableQuerySubscription<string>;
        let originalDirect: boolean;
        let originalTransport: QueryTransportMethod;
        let originalWebSocket: typeof WebSocket;
        let originalEventSourceFactory: typeof Globals.eventSourceFactory;
        const failure = {
            data: null,
            isSuccess: false,
            isReady: true,
            isAuthorized: true,
            isValid: true,
            hasExceptions: true,
            validationResults: [],
            exceptionMessages: ['The source failed'],
            exceptionStackTrace: '',
            paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 }
        };

        beforeEach(() => {
            originalDirect = Globals.queryDirectMode;
            originalTransport = Globals.queryTransportMethod;
            originalWebSocket = globalThis.WebSocket;
            originalEventSourceFactory = Globals.eventSourceFactory;
            Globals.queryDirectMode = true;
            Globals.queryTransportMethod = transport;
            callback = sinon.stub();
            const stream = {
                readyState: 1,
                onopen: null as (() => void) | null,
                onclose: null,
                onerror: null,
                onmessage: null as ((event: { data: string }) => void) | null,
                send: sinon.stub(),
                close: sinon.stub()
            };
            const socket = sinon.stub().returns(stream);
            Object.assign(socket, { CONNECTING: 0, OPEN: 1, CLOSING: 2, CLOSED: 3 });
            (globalThis as Record<string, unknown>)['WebSocket'] = socket;
            Globals.eventSourceFactory = () => stream as unknown as EventSource;
            context.query.setOrigin('https://example.com');
            subscription = context.query.subscribe(callback, { id: 'test-id' });
            stream.onopen?.();
            stream.onmessage?.({ data: JSON.stringify(failure) });
        });

        afterEach(() => {
            subscription?.unsubscribe();
            Globals.queryDirectMode = originalDirect;
            Globals.queryTransportMethod = originalTransport;
            globalThis.WebSocket = originalWebSocket;
            Globals.eventSourceFactory = originalEventSourceFactory;
            sinon.restore();
        });

        it('should deliver one failed result', () => {
            callback.should.have.been.calledOnce;
            callback.firstCall.args[0].isSuccess.should.be.false;
        });
        it('should expose the source error', () => callback.firstCall.args[0].exceptionMessages.should.deep.equal(['The source failed']));
        it('should settle readiness', () => callback.firstCall.args[0].isReady.should.be.true);
    }));
}
