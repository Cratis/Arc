// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { ServerSentEventQueryConnection } from '../../../ServerSentEventQueryConnection';
import { QueryResult } from '../../../QueryResult';

describe('when disconnecting prevents further message delivery', () => {
    let fakeEventSource: Record<string, unknown>;
    let connection: ServerSentEventQueryConnection<string[]>;
    let receivedCount: number;

    beforeEach(() => {
        fakeEventSource = { onmessage: null, onerror: null, close: sinon.stub() };

        (globalThis as Record<string, unknown>)['EventSource'] = function () {
            return fakeEventSource;
        };

        receivedCount = 0;
        connection = new ServerSentEventQueryConnection<string[]>(
            new URL('http://localhost/.cratis/queries/sse?query=Test')
        );
        connection.connect((_: QueryResult<string[]>) => { receivedCount++; });

        // Disconnect first, then send a message
        connection.disconnect();
        (fakeEventSource['onmessage'] as ((event: MessageEvent) => void) | null)?.(
            { data: JSON.stringify({ isSuccess: true }) } as MessageEvent
        );
    });

    afterEach(() => {
        delete (globalThis as Record<string, unknown>)['EventSource'];
        sinon.restore();
    });

    it('should not deliver messages after disconnect', () => receivedCount.should.equal(0));
});
