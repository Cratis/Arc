// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { ServerSentEventQueryConnection } from '../../ServerSentEventQueryConnection';
import { QueryResult } from '../../QueryResult';

/**
 * Unlike a WebSocket, an EventSource treats the server ending its response as the signal to retry - roughly every
 * three seconds, and each retry re-runs the entire query pipeline server side just to be denied again. The terminal
 * unauthorized result is only terminal if this client closes the stream itself.
 */
describe('when receiving unauthorized it terminates without reconnecting', () => {
    const unauthorizedResult = {
        data: null,
        isSuccess: false,
        isAuthorized: false,
        isValid: true,
        hasExceptions: false,
        validationResults: [],
        exceptionMessages: [],
        exceptionStackTrace: '',
        paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 }
    };

    let fakeEventSource: Record<string, unknown>;
    let closeStub: sinon.SinonStub;
    let constructedCount: number;
    let connection: ServerSentEventQueryConnection<string[]>;
    let receivedResults: QueryResult<string[]>[];

    beforeEach(() => {
        closeStub = sinon.stub();
        constructedCount = 0;
        fakeEventSource = { onmessage: null, onerror: null, close: closeStub };

        (globalThis as Record<string, unknown>)['EventSource'] = function () {
            constructedCount++;
            return fakeEventSource;
        };

        receivedResults = [];
        connection = new ServerSentEventQueryConnection<string[]>(new URL('http://localhost/api/queries/latest'));
        connection.connect((result: QueryResult<string[]>) => receivedResults.push(result));

        (fakeEventSource['onmessage'] as (event: MessageEvent) => void)(
            { data: JSON.stringify(unauthorizedResult) } as MessageEvent
        );

        // Anything that would normally bring the stream back up must now be refused.
        connection.connect(() => { });
    });

    afterEach(() => {
        delete (globalThis as Record<string, unknown>)['EventSource'];
        sinon.restore();
    });

    it('should deliver the unauthorized result', () => receivedResults.length.should.equal(1));
    it('should deliver it as not authorized', () => (receivedResults[0] as unknown as { isAuthorized: boolean }).isAuthorized.should.be.false);
    it('should close the event source', () => closeStub.calledOnce.should.be.true);
    it('should not re-establish the stream', () => constructedCount.should.equal(1));
});
