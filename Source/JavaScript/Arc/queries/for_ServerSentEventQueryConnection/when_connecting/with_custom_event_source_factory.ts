// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { Globals } from '../../../Globals';
import { EventSourceFactory } from '../../../EventSourceFactory';
import { ServerSentEventQueryConnection } from '../../ServerSentEventQueryConnection';
import { QueryResult } from '../../QueryResult';

interface FakeEventSource {
    onmessage: ((event: MessageEvent) => void) | null;
    onerror: (() => void) | null;
    close: sinon.SinonStub;
}

describe('when connecting with a custom event source factory configured and no global EventSource available', () => {
    let originalEventSource: typeof EventSource;
    let originalFactory: EventSourceFactory | undefined;
    let fakeEventSource: FakeEventSource;
    let factoryStub: sinon.SinonStub;
    let connection: ServerSentEventQueryConnection<string[]>;
    let receivedData: QueryResult<string[]>[];

    beforeEach(() => {
        originalEventSource = (globalThis as Record<string, unknown>)['EventSource'] as typeof EventSource;
        delete (globalThis as Record<string, unknown>)['EventSource'];

        originalFactory = Globals.eventSourceFactory;

        fakeEventSource = {
            onmessage: null,
            onerror: null,
            close: sinon.stub(),
        };
        factoryStub = sinon.stub().returns(fakeEventSource);
        Globals.eventSourceFactory = factoryStub as unknown as EventSourceFactory;

        receivedData = [];
        connection = new ServerSentEventQueryConnection<string[]>(new URL('http://localhost/.cratis/queries/sse?query=Test'));
        connection.connect((result: QueryResult<string[]>) => receivedData.push(result));
    });

    afterEach(() => {
        if (originalEventSource !== undefined) {
            (globalThis as Record<string, unknown>)['EventSource'] = originalEventSource;
        }
        Globals.eventSourceFactory = originalFactory;
        sinon.restore();
    });

    it('should call the custom factory instead of the global EventSource constructor', () => factoryStub.calledOnce.should.be.true);
    it('should pass the connection url to the factory', () => (factoryStub.getCall(0).args[0] as string).should.contain('/.cratis/queries/sse'));

    describe('when a message arrives on the custom event source', () => {
        const result = { data: ['a', 'b'], isSuccess: true, isAuthorized: true, isValid: true, hasExceptions: false, hasData: true, validationResults: [], exceptionMessages: [], exceptionStackTrace: '', paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 } };

        beforeEach(() => {
            fakeEventSource.onmessage!({ data: JSON.stringify(result) } as MessageEvent);
        });

        it('should deliver the payload to the callback', () => receivedData.length.should.equal(1));
    });
});
