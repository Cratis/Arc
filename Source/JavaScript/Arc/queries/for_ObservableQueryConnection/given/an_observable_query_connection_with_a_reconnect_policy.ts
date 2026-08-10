// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { ObservableQueryConnection } from '../../ObservableQueryConnection';
import { IReconnectPolicy } from '../../IReconnectPolicy';

/* eslint-disable @typescript-eslint/no-explicit-any */

export type FakeWebSocketWithPolicy = {
    onopen: (() => void) | null;
    onclose: (() => void) | null;
    onerror: ((error: unknown) => void) | null;
    onmessage: ((event: { data: string }) => void) | null;
    close: sinon.SinonStub;
    send: sinon.SinonStub;
    readyState: number;
};

export class an_observable_query_connection_with_a_reconnect_policy {
    connection: ObservableQueryConnection<unknown>;
    fakeWebSocket!: FakeWebSocketWithPolicy;
    reconnectPolicy: IReconnectPolicy;
    schedule: sinon.SinonStub;
    cancel: sinon.SinonStub;

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

        this.schedule = sinon.stub().returns(true);
        this.cancel = sinon.stub();
        this.reconnectPolicy = {
            attempt: 0,
            schedule: this.schedule,
            reset: sinon.stub(),
            cancel: this.cancel,
        } as unknown as IReconnectPolicy;

        this.connection = new ObservableQueryConnection<unknown>(
            new URL('https://example.com/api/test'),
            'test-microservice',
            10000,
            this.reconnectPolicy
        );
    }

    simulateMessage(payload: object): void {
        this.fakeWebSocket.onmessage?.({ data: JSON.stringify(payload) });
    }

    simulateClose(): void {
        this.fakeWebSocket.onclose?.();
    }
}
