// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { ServerSentEventQueryConnection } from '../../ServerSentEventQueryConnection';
import { QueryResult } from '../../QueryResult';

describe('when disconnecting closes the EventSource', () => {
    let closeStub: sinon.SinonStub;
    let connection: ServerSentEventQueryConnection<string[]>;

    beforeEach(() => {
        closeStub = sinon.stub();

        const FakeEventSourceClass = function (this: Record<string, unknown>) {
            this.onmessage = null;
            this.onerror = null;
            this.close = closeStub;
        };

        (globalThis as Record<string, unknown>)['EventSource'] = FakeEventSourceClass;

        connection = new ServerSentEventQueryConnection<string[]>(
            new URL('http://localhost/.cratis/queries/sse?query=Test')
        );
        connection.connect((_: QueryResult<string[]>) => _);
        connection.disconnect();
    });

    afterEach(() => {
        delete (globalThis as Record<string, unknown>)['EventSource'];
        sinon.restore();
    });

    it('should call close on the EventSource', () => closeStub.calledOnce.should.be.true);
});
