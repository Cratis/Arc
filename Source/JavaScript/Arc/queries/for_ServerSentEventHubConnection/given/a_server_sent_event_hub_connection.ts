// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { ServerSentEventHubConnection } from '../../ServerSentEventHubConnection';
import { IReconnectPolicy, ReconnectCallback } from '../../IReconnectPolicy';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class a_server_sent_event_hub_connection {
    connection: ServerSentEventHubConnection;
    fakeEventSource: {
        onopen: (() => void) | null;
        onmessage: ((event: MessageEvent) => void) | null;
        onerror: (() => void) | null;
        close: sinon.SinonStub;
        readyState: number;
    };
    policy: sinon.SinonStubbedInstance<IReconnectPolicy>;
    fetchStub: sinon.SinonStub;

    constructor() {
        this.setup();
    }

    /**
     * Re-initializes stubs and creates a fresh {@link ServerSentEventHubConnection}.
     * Call this at the start of each {@code beforeEach} to prevent stub-call
     * accumulation across tests sharing the same context instance.
     */
    setup(): void {
        this.fakeEventSource = {
            onopen: null,
            onmessage: null,
            onerror: null,
            close: sinon.stub(),
            readyState: 0, // CONNECTING
        };

        const self = this;
        const FakeEventSourceClass = function (this: any) {
            Object.assign(self.fakeEventSource, { onopen: null, onmessage: null, onerror: null });
            self.fakeEventSource.readyState = 0; // CONNECTING
            return self.fakeEventSource;
        };
        (globalThis as any)['EventSource'] = FakeEventSourceClass;
        (globalThis as any)['EventSource'].CONNECTING = 0;
        (globalThis as any)['EventSource'].OPEN = 1;
        (globalThis as any)['EventSource'].CLOSED = 2;

        this.fetchStub = sinon.stub().resolves({ ok: true });
        (globalThis as any)['fetch'] = this.fetchStub;

        this.policy = {
            attempt: 0,
            schedule: sinon.stub<[ReconnectCallback, string], boolean>().returns(true),
            reset: sinon.stub(),
            cancel: sinon.stub(),
        };

        this.connection = new ServerSentEventHubConnection(
            'http://localhost/.cratis/queries/sse',
            'http://localhost/.cratis/queries/sse/subscribe',
            'http://localhost/.cratis/queries/sse/unsubscribe',
            '',
            this.policy
        );
    }

    simulateOpen(): void {
        this.fakeEventSource.readyState = 1; // OPEN
        this.fakeEventSource.onopen?.();
    }

    simulateMessage(payload: object): void {
        this.fakeEventSource.onmessage?.({ data: JSON.stringify(payload) } as MessageEvent);
    }

    simulateError(): void {
        this.fakeEventSource.onerror?.();
    }
}
