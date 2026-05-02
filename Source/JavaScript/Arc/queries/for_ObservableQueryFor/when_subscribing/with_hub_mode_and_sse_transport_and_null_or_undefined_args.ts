// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { an_observable_query_for } from '../given/an_observable_query_for';
import { given } from '../../../given';
import { Globals } from '../../../Globals';
import { QueryTransportMethod } from '../../QueryTransportMethod';
import { ObservableQuerySubscription } from '../../ObservableQuerySubscription';
import { HubMessageType } from '../../WebSocketHubConnection';
import { resetSharedMultiplexer } from '../../ObservableQueryMultiplexer';

import * as sinon from 'sinon';

describe('when subscribing with hub mode and SSE transport and null or undefined args', given(an_observable_query_for, context => {
    let callback: sinon.SinonStub;
    let subscription: ObservableQuerySubscription<string>;
    let fetchStub: sinon.SinonStub;
    let originalQueryDirectMode: boolean;
    let originalTransportMethod: QueryTransportMethod;
    let eventSourceInstance: EventSource & {
        onopen: (() => void) | null;
        onmessage: ((event: { data: string }) => void) | null;
    };

    beforeEach(() => {
        originalQueryDirectMode = Globals.queryDirectMode;
        originalTransportMethod = Globals.queryTransportMethod;
        resetSharedMultiplexer();

        Globals.queryDirectMode = false;
        Globals.queryTransportMethod = QueryTransportMethod.ServerSentEvents;

        context.queryWithParameterDescriptorValues.setOrigin('https://example.com');
        callback = sinon.stub();

        const FakeEventSourceConstructor = function (this: EventSource, url: string) {
            void url;
            eventSourceInstance = this as EventSource & {
                onopen: (() => void) | null;
                onmessage: ((event: { data: string }) => void) | null;
            };
            Object.assign(this, {
                onopen: null,
                onerror: null,
                onmessage: null,
                close: sinon.stub(),
                addEventListener: sinon.stub(),
                removeEventListener: sinon.stub(),
                readyState: 0,
            });
        };
        (globalThis as Record<string, unknown>)['EventSource'] = FakeEventSourceConstructor;

        fetchStub = sinon.stub().resolves({ ok: true } as Response);
        (globalThis as Record<string, unknown>)['fetch'] = fetchStub;

        subscription = context.queryWithParameterDescriptorValues.subscribe(callback, {
            filter: undefined,
            limit: null
        });

        eventSourceInstance.onopen?.();
        eventSourceInstance.onmessage?.({
            data: JSON.stringify({
                type: HubMessageType.Connected,
                payload: 'conn-1'
            })
        });
    });

    afterEach(() => {
        Globals.queryDirectMode = originalQueryDirectMode;
        Globals.queryTransportMethod = originalTransportMethod;
        if (subscription) {
            subscription.unsubscribe();
        }
        delete (globalThis as Record<string, unknown>)['EventSource'];
        delete (globalThis as Record<string, unknown>)['fetch'];
        resetSharedMultiplexer();
        sinon.restore();
    });

    it('should not include null or undefined arguments in the subscribe request', () => {
        fetchStub.called.should.be.true;
        const subscribeBody = JSON.parse(fetchStub.firstCall.args[1].body as string);
        (subscribeBody.request.arguments === undefined).should.be.true;
    });
}));
