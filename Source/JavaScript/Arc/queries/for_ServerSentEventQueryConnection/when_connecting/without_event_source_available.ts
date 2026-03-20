// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { ServerSentEventQueryConnection } from '../../ServerSentEventQueryConnection';
import { QueryResult } from '../../QueryResult';

describe('when connecting in an environment where EventSource is not available', () => {
    let originalEventSource: typeof EventSource;
    let connection: ServerSentEventQueryConnection<string[]>;
    let dataReceivedStub: sinon.SinonStub;

    beforeEach(() => {
        originalEventSource = (globalThis as Record<string, unknown>)['EventSource'] as typeof EventSource;
        delete (globalThis as Record<string, unknown>)['EventSource'];

        dataReceivedStub = sinon.stub();
        connection = new ServerSentEventQueryConnection<string[]>(new URL('http://localhost/.cratis/queries/sse?query=Test'));
        connection.connect(dataReceivedStub as unknown as (result: QueryResult<string[]>) => void);
    });

    afterEach(() => {
        if (originalEventSource !== undefined) {
            (globalThis as Record<string, unknown>)['EventSource'] = originalEventSource;
        }
        sinon.restore();
    });

    it('should not call data received', () => dataReceivedStub.called.should.be.false);
    it('should not throw', () => true.should.be.true);
});
