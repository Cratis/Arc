// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_descriptor } from '../given/a_descriptor';
import { given } from '../../../given';
import { Globals } from '../../../Globals';
import { QueryTransportMethod } from '../../QueryTransportMethod';
import { createObservableQueryConnection } from '../../ObservableQueryConnectionFactory';
import { MultiplexedObservableQueryConnection, resetSharedMultiplexer } from '../../ObservableQueryMultiplexer';
import { IObservableQueryConnection } from '../../IObservableQueryConnection';

import * as sinon from 'sinon';

describe('when creating with hub mode and SSE transport and connection count within safe limit', given(a_descriptor, context => {
    let connection: IObservableQueryConnection<unknown>;
    let warnStub: sinon.SinonStub;
    let originalConnectionCount: number;

    beforeEach(() => {
        originalConnectionCount = Globals.queryConnectionCount;
        Globals.queryDirectMode = false;
        Globals.queryTransportMethod = QueryTransportMethod.ServerSentEvents;
        Globals.queryConnectionCount = 4;

        warnStub = sinon.stub(console, 'warn');

        const FakeEventSourceConstructor = function (this: EventSource) {
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
        (globalThis as Record<string, unknown>)['fetch'] = sinon.stub().resolves({ ok: true } as Response);

        connection = createObservableQueryConnection(context.descriptor);
    });

    afterEach(() => {
        Globals.queryDirectMode = context.originalDirectMode;
        Globals.queryTransportMethod = context.originalTransportMethod;
        Globals.queryConnectionCount = originalConnectionCount;
        warnStub.restore();
        delete (globalThis as Record<string, unknown>)['EventSource'];
        delete (globalThis as Record<string, unknown>)['fetch'];
        resetSharedMultiplexer();
    });

    it('should return a MultiplexedObservableQueryConnection', () => {
        connection.should.be.instanceOf(MultiplexedObservableQueryConnection);
    });

    it('should not log a warning', () => {
        warnStub.called.should.be.false;
    });
}));
