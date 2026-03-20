// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { ServerSentEventQueryConnection } from '../../ServerSentEventQueryConnection';
import { QueryResult } from '../../QueryResult';

interface FakeEventSource {
    url: string;
    onmessage: ((event: MessageEvent) => void) | null;
    onerror: (() => void) | null;
    close: sinon.SinonStub;
}

describe('when connecting with query arguments', () => {
    let fakeEventSource: FakeEventSource;
    let connection: ServerSentEventQueryConnection<string[]>;
    let receivedData: QueryResult<string[]>[];

    beforeEach(() => {
        fakeEventSource = {
            url: '',
            onmessage: null,
            onerror: null,
            close: sinon.stub(),
        };

        const FakeEventSourceClass = function (this: FakeEventSource, url: string) {
            fakeEventSource.url = url;
            Object.assign(fakeEventSource, { onmessage: null, onerror: null });
            return fakeEventSource;
        };

        (globalThis as Record<string, unknown>)['EventSource'] = FakeEventSourceClass;

        receivedData = [];
        connection = new ServerSentEventQueryConnection<string[]>(
            new URL('http://localhost/.cratis/queries/sse?query=Test')
        );
        connection.connect(
            (result: QueryResult<string[]>) => receivedData.push(result),
            { category: 'books' }
        );
    });

    afterEach(() => {
        delete (globalThis as Record<string, unknown>)['EventSource'];
        sinon.restore();
    });

    it('should append query arguments to the URL', () => fakeEventSource.url.should.contain('category=books'));

    describe('when a message arrives', () => {
        const payload = { data: ['a', 'b'], isSuccess: true, isAuthorized: true, isValid: true, hasExceptions: false, validationResults: [], exceptionMessages: [], exceptionStackTrace: '' };

        beforeEach(() => {
            fakeEventSource.onmessage!({ data: JSON.stringify({ payload }) } as MessageEvent);
        });

        it('should deliver the payload to the callback', () => receivedData.length.should.equal(1));
        it('should extract the inner payload from the hub envelope', () => receivedData[0]!.isSuccess.should.be.true);
    });
});
