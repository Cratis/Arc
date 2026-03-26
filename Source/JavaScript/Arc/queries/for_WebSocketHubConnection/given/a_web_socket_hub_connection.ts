// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { WebSocketHubConnection } from '../../WebSocketHubConnection';
import { IReconnectPolicy } from '../../IReconnectPolicy';
import { ReconnectCallback } from '../../IReconnectPolicy';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class a_web_socket_hub_connection {
    connection!: WebSocketHubConnection;
    fakeSocket!: {
        onopen: (() => void) | null;
        onclose: (() => void) | null;
        onerror: ((error: Event) => void) | null;
        onmessage: ((ev: MessageEvent) => void) | null;
        send: sinon.SinonStub;
        close: sinon.SinonStub;
        readyState: number;
    };
    policy!: sinon.SinonStubbedInstance<IReconnectPolicy>;
    WebSocketStub!: sinon.SinonStub;

    constructor() {
        this.setup();
    }

    /**
     * Re-initializes stubs and creates a fresh {@link WebSocketHubConnection}.
     * Call this at the start of each {@code beforeEach} to prevent stub-call
     * accumulation across tests sharing the same context instance.
     */
    setup(): void {
        this.fakeSocket = {
            onopen: null,
            onclose: null,
            onerror: null,
            onmessage: null,
            send: sinon.stub(),
            close: sinon.stub(),
            readyState: 0, // CONNECTING
        };

        this.WebSocketStub = sinon.stub().returns(this.fakeSocket);
        (globalThis as any)['WebSocket'] = this.WebSocketStub;
        // WebSocket ready-state constants (numeric values matching the spec)
        (globalThis as any)['WebSocket'].CONNECTING = 0;
        (globalThis as any)['WebSocket'].OPEN = 1;
        (globalThis as any)['WebSocket'].CLOSING = 2;
        (globalThis as any)['WebSocket'].CLOSED = 3;

        this.policy = {
            attempt: 0,
            schedule: sinon.stub<[ReconnectCallback, string], boolean>().returns(true),
            reset: sinon.stub(),
            cancel: sinon.stub(),
        };

        this.connection = new WebSocketHubConnection(
            'ws://localhost/.cratis/queries/ws',
            '',
            undefined,
            this.policy
        );
    }

    simulateOpen(): void {
        this.fakeSocket.readyState = 1; // OPEN
        this.fakeSocket.onopen?.();
    }

    simulateClose(): void {
        this.fakeSocket.readyState = 3; // CLOSED
        this.fakeSocket.onclose?.();
    }

    simulateMessage(payload: object): void {
        this.fakeSocket.onmessage?.({ data: JSON.stringify(payload) } as MessageEvent);
    }
}
