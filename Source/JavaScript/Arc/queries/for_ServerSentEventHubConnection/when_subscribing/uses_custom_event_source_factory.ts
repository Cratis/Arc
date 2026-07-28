// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { Globals } from '../../../Globals';
import { EventSourceFactory } from '../../../EventSourceFactory';
import { a_server_sent_event_hub_connection } from '../given/a_server_sent_event_hub_connection';
import { given } from '../../../given';
import { HubMessageType } from '../../WebSocketHubConnection';

interface FakeEventSource {
    onopen: (() => void) | null;
    onmessage: ((event: MessageEvent) => void) | null;
    onerror: (() => void) | null;
    close: sinon.SinonStub;
    readyState: number;
}

describe('when subscribing with a custom event source factory configured', given(a_server_sent_event_hub_connection, context => {
    let customEventSource: FakeEventSource;
    let factoryStub: sinon.SinonStub;
    let originalFactory: EventSourceFactory | undefined;

    beforeEach(() => {
        originalFactory = Globals.eventSourceFactory;

        customEventSource = {
            onopen: null,
            onmessage: null,
            onerror: null,
            close: sinon.stub(),
            readyState: 1, // OPEN
        };
        factoryStub = sinon.stub().returns(customEventSource);
        Globals.eventSourceFactory = factoryStub as unknown as EventSourceFactory;

        context.setup();
        context.connection.subscribe('q1', { queryName: 'MyQuery' }, sinon.stub());
    });

    afterEach(() => {
        Globals.eventSourceFactory = originalFactory;
        sinon.restore();
    });

    it('should call the custom factory with the SSE hub url', () => factoryStub.calledOnce.should.be.true);
    it('should pass the hub url to the factory', () => factoryStub.getCall(0).args[0].should.equal('http://localhost/.cratis/queries/sse'));
    it('should not use the default global EventSource', () => (context.fakeEventSource.onopen === null).should.be.true);

    describe('when the custom event source receives the Connected message', () => {
        beforeEach(() => {
            customEventSource.onmessage!({ data: JSON.stringify({ type: HubMessageType.Connected, payload: 'conn-1' }) } as MessageEvent);
        });

        it('should record the connection as open', () => context.connection.isConnected.should.be.true);
    });
}));
