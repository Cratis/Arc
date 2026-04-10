// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { ObservableQueryConnection } from '../../ObservableQueryConnection';

/* eslint-disable @typescript-eslint/no-explicit-any */

export type FakeWebSocket = {
    onopen: (() => void) | null;
    onclose: (() => void) | null;
    onerror: ((error: unknown) => void) | null;
    onmessage: ((event: { data: string }) => void) | null;
    close: sinon.SinonStub;
    send: sinon.SinonStub;
    readyState: number;
};

export class an_observable_query_connection_with_websocket {
    connection: ObservableQueryConnection<unknown>;
    fakeWebSocket!: FakeWebSocket;

    constructor() {
        this.fakeWebSocket = {
            onopen: null,
            onclose: null,
            onerror: null,
            onmessage: null,
            close: sinon.stub(),
            send: sinon.stub(),
            readyState: WebSocket.OPEN,
        };

        const fakeWebSocket = this.fakeWebSocket;
        const FakeWebSocketClass = function (this: any) {
            fakeWebSocket.onopen = null;
            fakeWebSocket.onclose = null;
            fakeWebSocket.onerror = null;
            fakeWebSocket.onmessage = null;
            return fakeWebSocket;
        };
        (globalThis as any)['WebSocket'] = FakeWebSocketClass;

        this.connection = new ObservableQueryConnection<unknown>(
            new URL('https://example.com/api/test'),
            'test-microservice'
        );
    }

    simulateMessage(payload: object): void {
        this.fakeWebSocket.onmessage?.({ data: JSON.stringify(payload) });
    }

    simulateOpen(): void {
        this.fakeWebSocket.readyState = WebSocket.OPEN;
        this.fakeWebSocket.onopen?.();
    }
}
